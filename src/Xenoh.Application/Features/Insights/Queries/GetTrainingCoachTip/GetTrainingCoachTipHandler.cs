using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Insights.Queries.GetTrainingCoachTip;

public sealed class GetTrainingCoachTipHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<GetTrainingCoachTipQuery, TrainingCoachTipResponse>
{
    private const string FeatureName = "training-coach-tip";
    private const int PromptVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<TrainingCoachTipResponse> Handle(
        GetTrainingCoachTipQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var language = NormalizeLanguage(request.Language);
        var snapshot = await BuildSnapshotAsync(userId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var fingerprint = ComputeFingerprint(snapshotJson, language);

        var cache = await db.AiFeatureCaches.FirstOrDefaultAsync(c =>
            c.Feature == FeatureName &&
            c.Language == language &&
            c.UserId == userId &&
            c.SubjectUserId == null &&
            c.ResourceId == null,
            cancellationToken);

        if (cache is not null && cache.DataFingerprint == fingerprint)
            return ToResponse(language, cache.GeneratedAt, true, cache.ContentJson);

        var aiResult = await ai.GenerateTrainingCoachTipAsync(
            new TrainingCoachTipAiRequest(language, snapshotJson),
            cancellationToken);

        var content = ParseContent(aiResult.Json);
        var contentJson = JsonSerializer.Serialize(content, JsonOptions);
        var now = DateTime.UtcNow;

        if (cache is null)
        {
            db.AiFeatureCaches.Add(new AiFeatureCache
            {
                Feature = FeatureName,
                Language = language,
                UserId = userId,
                DataFingerprint = fingerprint,
                ContentJson = contentJson,
                GeneratedAt = now,
            });
        }
        else
        {
            cache.DataFingerprint = fingerprint;
            cache.ContentJson = contentJson;
            cache.GeneratedAt = now;
            cache.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(language, now, false, contentJson);
    }

    private async Task<object> BuildSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-28);

        var profile = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Gender = u.Gender.HasValue ? u.Gender.Value.ToString() : null,
                u.DateOfBirth,
                HeightCm = u.Height,
                DevelopmentDirection = u.DevelopmentDirection.HasValue
                    ? u.DevelopmentDirection.Value.ToString()
                    : null,
                TrainingDiscipline = u.TrainingDiscipline.HasValue
                    ? u.TrainingDiscipline.Value.ToString()
                    : null
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("User not found.");

        var activePlan = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.IsActive)
            .Select(p => new PlanSnapshotData(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.WeeklyWorkouts
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new WeekSnapshotData(
                        w.Id,
                        w.WeekNumber,
                        w.Name,
                        w.StartDate,
                        w.EndDate,
                        w.DailyWorkouts.Select(d => new DaySnapshotData(
                            d.Id,
                            d.Date,
                            d.Status,
                            d.IsCompleted,
                            d.Exercises.Count,
                            d.Exercises.Count(e => e.IsCompleted),
                            d.Exercises.SelectMany(e => e.Sets).Any(s =>
                                s.IsCompleted &&
                                ((s.ActualReps.HasValue && s.ActualReps.Value < s.PlannedReps) ||
                                 (s.ActualWeight.HasValue && s.PlannedWeight.HasValue && s.ActualWeight.Value < s.PlannedWeight.Value)))
                        )).ToList()
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since)
            .Select(s => new RecentSetSnapshot(
                s.Exercise.DailyWorkout.Date,
                s.Exercise.Name,
                s.Exercise.PrimaryMuscleGroup.ToString(),
                s.PlannedReps,
                s.PlannedWeight,
                s.ActualReps,
                s.ActualWeight,
                s.Rpe))
            .ToListAsync(ct);

        var recentSetFacts = recentSets
            .OrderByDescending(s => s.Date)
            .Take(80)
            .ToList();

        var recentVolume = recentSets.Sum(SetVolume);
        var currentWeek = activePlan?.Weeks.FirstOrDefault(w => w.StartDate <= today && today <= w.EndDate)
            ?? activePlan?.Weeks.LastOrDefault(w => w.StartDate <= today);
        var previousWeek = currentWeek is null
            ? null
            : activePlan?.Weeks.LastOrDefault(w => w.WeekNumber == currentWeek.WeekNumber - 1);

        var planSummary = activePlan is null
            ? null
            : new
            {
                activePlan.Name,
                activePlan.StartDate,
                activePlan.EndDate,
                TotalDays = activePlan.Weeks.Sum(w => w.Days.Count),
                CompletedDays = activePlan.Weeks.Sum(w => w.Days.Count(d => d.IsCompleted)),
                MissedDays = activePlan.Weeks.Sum(w => w.Days.Count(d => d.Status == DayStatus.Missed)),
                CurrentWeek = ToWeekSnapshot(currentWeek, recentSets),
                PreviousWeek = ToWeekSnapshot(previousWeek, recentSets)
            };

        var muscleVolumes = BuildMuscleVolume(recentSets);
        var ruleInsights = BuildRuleInsights(activePlan, recentSets);
        var powerliftingInsights = ShouldUsePowerliftingRules(profile.TrainingDiscipline)
            ? BuildPowerliftingInsights(recentSets)
            : [];

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Join(
                db.ExerciseTemplates.AsNoTracking(),
                p => p.ExerciseTemplateId,
                t => t.Id,
                (p, t) => new { Exercise = t.Name, p.Weight, p.Reps, p.AchievedAt })
            .OrderByDescending(p => p.AchievedAt)
            .Take(5)
            .ToListAsync(ct);

        return new
        {
            AsOf = today,
            PromptVersion,
            IsSparse = activePlan is null && recentSets.Count == 0,
            ProfileContext = profile,
            ActivePlan = planSummary,
            Recent28Days = new
            {
                CompletedSets = recentSets.Count,
                TotalVolume = Math.Round(recentVolume, 1),
                AverageRpe = recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
                HighRpeSets = recentSets.Count(s => s.Rpe >= 8.5m),
                MissedTargetSets = recentSets.Count(IsMissedTarget),
                LowRpeWins = recentSets.Count(s => s.Rpe <= 7m && IsAtOrAboveTarget(s)),
                MuscleVolume = muscleVolumes,
                Sets = recentSetFacts
            },
            RuleInsights = ruleInsights.Concat(powerliftingInsights).Take(8).ToList(),
            RecentPrs = prs
        };
    }

    private static object? ToWeekSnapshot(WeekSnapshotData? week, IReadOnlyList<RecentSetSnapshot> recentSets)
    {
        if (week is null) return null;

        var weekSets = recentSets
            .Where(s => week.StartDate <= s.Date && s.Date <= week.EndDate)
            .ToList();

        return new
        {
            week.WeekNumber,
            week.Name,
            week.StartDate,
            week.EndDate,
            TotalDays = week.Days.Count,
            CompletedDays = week.Days.Count(d => d.IsCompleted),
            MissedDays = week.Days.Count(d => d.Status == DayStatus.Missed),
            WarningDays = week.Days.Count(d => d.HasMissedTargets),
            TotalExercises = week.Days.Sum(d => d.ExerciseCount),
            CompletedExercises = week.Days.Sum(d => d.CompletedExercises),
            Volume = Math.Round(weekSets.Sum(SetVolume), 1)
        };
    }

    private static List<object> BuildMuscleVolume(IReadOnlyList<RecentSetSnapshot> recentSets)
    {
        var totalVolume = recentSets.Sum(SetVolume);

        return recentSets
            .GroupBy(s => s.Muscle)
            .Select(g =>
            {
                var volume = g.Sum(SetVolume);
                return new
                {
                    Muscle = g.Key,
                    Sets = g.Count(),
                    Volume = Math.Round(volume, 1),
                    PercentOfTotal = totalVolume <= 0m
                        ? 0m
                        : Math.Round(volume / totalVolume * 100m, 1)
                };
            })
            .OrderByDescending(m => m.Volume)
            .Take(10)
            .Cast<object>()
            .ToList();
    }

    private static List<object> BuildRuleInsights(
        PlanSnapshotData? activePlan,
        IReadOnlyList<RecentSetSnapshot> recentSets)
    {
        var totalDays = activePlan is null ? 0 : activePlan.Weeks.Sum(w => w.Days.Count);
        var completedDays = activePlan is null ? 0 : activePlan.Weeks.Sum(w => w.Days.Count(d => d.IsCompleted));
        var missedDays = activePlan is null ? 0 : activePlan.Weeks.Sum(w => w.Days.Count(d => d.Status == DayStatus.Missed));
        var warningDays = activePlan is null ? 0 : activePlan.Weeks.Sum(w => w.Days.Count(d => d.HasMissedTargets));

        var weekVolumes = new List<WeekVolumePoint>();
        if (activePlan is not null)
        {
            foreach (var week in activePlan.Weeks)
            {
                var volume = recentSets
                    .Where(s => week.StartDate <= s.Date && s.Date <= week.EndDate)
                    .Sum(SetVolume);
                weekVolumes.Add(new WeekVolumePoint(week.WeekNumber, week.Name, Math.Round(volume, 1)));
            }
        }

        var totalVolume = recentSets.Sum(SetVolume);
        var musclePoints = recentSets
            .GroupBy(s => s.Muscle)
            .Select(g =>
            {
                var volume = g.Sum(SetVolume);
                var percent = totalVolume <= 0m ? 0m : Math.Round(volume / totalVolume * 100m, 1);
                return new MuscleGroupPoint(g.Key, g.Count(), volume, volume, 0m, percent);
            })
            .ToList();

        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            totalDays <= 0 ? 0m : Math.Round(completedDays * 100m / totalDays, 1),
            totalDays,
            missedDays,
            warningDays,
            recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
            recentSets.Count(s => s.Rpe >= 8.5m),
            weekVolumes,
            musclePoints));

        return result.Insights
            .Select(i => new
            {
                i.Type,
                i.Severity,
                i.Title,
                i.Message,
                i.MetricLabel,
                i.MetricValue
            })
            .Cast<object>()
            .ToList();
    }

    private static List<object> BuildPowerliftingInsights(IReadOnlyList<RecentSetSnapshot> recentSets)
    {
        var lifts = new List<LiftProgressionResult>();

        foreach (var lift in new[] { CompetitionLiftType.Squat, CompetitionLiftType.Bench, CompetitionLiftType.Deadlift })
        {
            var best = recentSets
                .Where(s => MatchesLift(s.Exercise, lift))
                .Select(s =>
                {
                    var reps = s.ActualReps ?? s.PlannedReps;
                    var weight = s.ActualWeight ?? s.PlannedWeight ?? 0m;
                    var e1Rm = OneRepMaxCalculator.Estimate(weight, reps);
                    return e1Rm is null ? null : new { E1Rm = e1Rm.Value };
                })
                .Where(x => x is not null)
                .OrderByDescending(x => x!.E1Rm)
                .FirstOrDefault();

            if (best is null) continue;

            lifts.Add(new LiftProgressionResult(
                lift,
                E1RmSeries: [],
                PrTimeline: [],
                CurrentE1Rm: best.E1Rm,
                CurrentTrainingMax: OneRepMaxCalculator.TrainingMax(best.E1Rm),
                IsPlateau: false));
        }

        var highRpeWeekStreak = CalculateHighRpeWeekStreak(recentSets);

        return PowerliftingInsightRules
            .Evaluate(new PowerliftingInsightInput(lifts, highRpeWeekStreak))
            .Select(i => new
            {
                i.Type,
                i.Severity,
                i.Title,
                i.Message,
                i.MetricLabel,
                i.MetricValue
            })
            .Cast<object>()
            .ToList();
    }

    private static int CalculateHighRpeWeekStreak(IReadOnlyList<RecentSetSnapshot> recentSets)
    {
        var streak = 0;

        foreach (var week in recentSets
            .Where(s => s.Rpe.HasValue)
            .GroupBy(s => WeekStart(s.Date))
            .OrderByDescending(g => g.Key))
        {
            var average = week.Average(s => s.Rpe!.Value);
            if (average < 8.5m) break;
            streak++;
        }

        return streak;
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static bool ShouldUsePowerliftingRules(string? trainingDiscipline) =>
        string.Equals(trainingDiscipline, TrainingDiscipline.Powerlifting.ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool MatchesLift(string exerciseName, CompetitionLiftType lift)
    {
        var name = exerciseName.ToLowerInvariant();
        return lift switch
        {
            CompetitionLiftType.Squat => name.Contains("squat"),
            CompetitionLiftType.Bench => name.Contains("bench"),
            CompetitionLiftType.Deadlift => name.Contains("deadlift") || name.Contains("dead lift"),
            _ => false
        };
    }

    private static decimal SetVolume(RecentSetSnapshot set)
    {
        var reps = set.ActualReps ?? set.PlannedReps;
        var weight = set.ActualWeight ?? set.PlannedWeight ?? 0m;
        return reps * weight;
    }

    private static bool IsMissedTarget(RecentSetSnapshot set) =>
        (set.ActualReps is not null && set.ActualReps < set.PlannedReps) ||
        (set.ActualWeight is not null && set.PlannedWeight is not null && set.ActualWeight < set.PlannedWeight);

    private static bool IsAtOrAboveTarget(RecentSetSnapshot set) =>
        (set.ActualReps is null || set.ActualReps >= set.PlannedReps) &&
        (set.ActualWeight is null || set.PlannedWeight is null || set.ActualWeight >= set.PlannedWeight);

    private static TrainingCoachTipContent ParseContent(string json)
    {
        TrainingCoachTipContent? content;
        try
        {
            content = JsonSerializer.Deserialize<TrainingCoachTipContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("AI returned malformed JSON.", ex);
        }

        if (content is null)
            throw new InvalidOperationException("AI returned malformed JSON.");

        return new TrainingCoachTipContent(
            TrimOrFallback(content.Headline, "Training tip"),
            NormalizeCategory(content.Category),
            TrimOrFallback(content.Insight, "Log more training data so Xenoh Coach can give a sharper tip."),
            (content.Evidence ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Take(5)
                .ToList(),
            TrimOrFallback(content.WhyItMatters, "More complete logs improve coaching accuracy."),
            TrimOrFallback(content.NextAction, "Log your next workout with reps, weight, and RPE."),
            NormalizeConfidence(content.Confidence));
    }

    private static TrainingCoachTipResponse ToResponse(string language, DateTime generatedAt, bool cached, string json)
    {
        var content = ParseContent(json);
        return new TrainingCoachTipResponse(
            language,
            generatedAt,
            cached,
            content.Headline!,
            content.Category!,
            content.Insight!,
            content.Evidence!,
            content.WhyItMatters!,
            content.NextAction!,
            content.Confidence!);
    }

    private static string TrimOrFallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

    private static string NormalizeCategory(string? category)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Technique",
            "Progression",
            "Recovery",
            "Adherence",
            "Volume",
            "Powerlifting",
            "General"
        };

        return !string.IsNullOrWhiteSpace(category) && allowed.Contains(category)
            ? allowed.First(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
            : "General";
    }

    private static string NormalizeConfidence(string? confidence) =>
        string.Equals(confidence, "High", StringComparison.OrdinalIgnoreCase) ? "High"
        : string.Equals(confidence, "Low", StringComparison.OrdinalIgnoreCase) ? "Low"
        : "Moderate";

    private static string ComputeFingerprint(string snapshotJson, string language)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{FeatureName}:v{PromptVersion}:{language}:{snapshotJson}"));
        return Convert.ToHexString(hash);
    }

    private sealed record RecentSetSnapshot(
        DateOnly Date,
        string Exercise,
        string Muscle,
        int PlannedReps,
        decimal? PlannedWeight,
        int? ActualReps,
        decimal? ActualWeight,
        decimal? Rpe
    );

    private sealed record PlanSnapshotData(
        Guid Id,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        List<WeekSnapshotData> Weeks
    );

    private sealed record WeekSnapshotData(
        Guid Id,
        int WeekNumber,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        List<DaySnapshotData> Days
    );

    private sealed record DaySnapshotData(
        Guid Id,
        DateOnly Date,
        DayStatus Status,
        bool IsCompleted,
        int ExerciseCount,
        int CompletedExercises,
        bool HasMissedTargets
    );

    private sealed record TrainingCoachTipContent(
        string? Headline,
        string? Category,
        string? Insight,
        IReadOnlyList<string>? Evidence,
        string? WhyItMatters,
        string? NextAction,
        string? Confidence
    );
}
