using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;

public sealed class GetUserAnalysisHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IUserAnalysisAi ai
) : IRequestHandler<GetUserAnalysisQuery, UserAnalysisResponse>
{
    private const int AnalysisPromptVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<UserAnalysisResponse> Handle(
        GetUserAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

        var snapshot = await BuildSnapshotAsync(userId, cancellationToken);
        var fingerprint = ComputeFingerprint(snapshot, language);

        var existing = await db.UserAnalyses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Language == language, cancellationToken);

        if (existing is not null && existing.DataFingerprint == fingerprint)
        {
            var cachedContent = JsonSerializer.Deserialize<AnalysisContent>(existing.ContentJson, JsonOptions)
                ?? throw new InvalidOperationException("Cached analysis is corrupted.");
            return new UserAnalysisResponse(language, existing.GeneratedAt, Cached: true, cachedContent, BuildMetrics(snapshot));
        }

        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var aiResult = await ai.GenerateAsync(new UserAnalysisAiRequest(language, snapshotJson), cancellationToken);

        var content = JsonSerializer.Deserialize<AnalysisContent>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        var contentJson = JsonSerializer.Serialize(content, JsonOptions);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            db.UserAnalyses.Add(new UserAnalysis
            {
                UserId = userId,
                Language = language,
                DataFingerprint = fingerprint,
                ContentJson = contentJson,
                GeneratedAt = now,
            });
        }
        else
        {
            existing.DataFingerprint = fingerprint;
            existing.ContentJson = contentJson;
            existing.GeneratedAt = now;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UserAnalysisResponse(language, now, Cached: false, content, BuildMetrics(snapshot));
    }

    private static AnalysisMetrics BuildMetrics(Snapshot snapshot)
    {
        var planCompletionPercent = Percent(snapshot.Plan?.CompletedDays ?? 0, snapshot.Plan?.TotalDays ?? 0);
        var bodyweightPoints = snapshot.RecentBodyweight
            .OrderBy(p => p.Date)
            .Select(p => new AnalysisBodyweightPoint(p.Date, p.Weight))
            .ToList();

        decimal? latestWeight = bodyweightPoints.LastOrDefault()?.Weight;
        decimal? delta = bodyweightPoints.Count >= 2
            ? Math.Round(bodyweightPoints[^1].Weight - bodyweightPoints[0].Weight, 1)
            : null;

        var totalMuscleVolume = snapshot.MuscleVolumes.Sum(m => m.Volume);
        var totalMuscleSets = snapshot.MuscleVolumes.Sum(m => m.Sets);
        var muscleBalance = snapshot.MuscleVolumes
            .Select(m =>
            {
                var share = totalMuscleVolume > 0
                    ? Percent(m.Volume, totalMuscleVolume)
                    : Percent(m.Sets, totalMuscleSets);
                return new AnalysisMuscleBalancePoint(m.Muscle, m.Sets, Math.Round(m.Volume, 1), share);
            })
            .ToList();

        return new AnalysisMetrics(
            new AnalysisAdherenceMetrics(
                snapshot.Plan?.Name,
                snapshot.Plan?.StartDate,
                snapshot.Plan?.EndDate,
                snapshot.Plan?.TotalDays ?? 0,
                snapshot.Plan?.CompletedDays ?? 0,
                planCompletionPercent,
                ToWeekComparison(snapshot.CurrentWeek, snapshot.CurrentWeekVolume),
                ToWeekComparison(snapshot.PreviousWeek, snapshot.PreviousWeekVolume)
            ),
            new AnalysisBodyweightMetrics(
                bodyweightPoints,
                latestWeight,
                delta,
                delta is null ? "Unknown" : delta > 0 ? "Up" : delta < 0 ? "Down" : "Stable"
            ),
            new AnalysisVolumeMetrics(
                Math.Round(snapshot.RecentVolume, 1),
                snapshot.RecentSetCount,
                Math.Round(snapshot.CurrentWeekVolume, 1),
                Math.Round(snapshot.PreviousWeekVolume, 1),
                Math.Round(snapshot.CurrentWeekVolume - snapshot.PreviousWeekVolume, 1)
            ),
            muscleBalance,
            new AnalysisEffortGapMetrics(
                snapshot.HighRpeMisses
                    .Select(e => new AnalysisEffortGapPoint(e.Exercise, e.Sets, e.AverageRpe, e.Pattern))
                    .ToList(),
                snapshot.LowRpeWins
                    .Select(e => new AnalysisEffortGapPoint(e.Exercise, e.Sets, e.AverageRpe, e.Pattern))
                    .ToList()
            ),
            snapshot.RecentPrs
                .Select(p => new AnalysisRecentPrEntry(p.Exercise, p.Weight, p.Reps, p.AchievedAt))
                .ToList()
        );
    }

    private static AnalysisWeekComparison? ToWeekComparison(WeekSnapshot? week, decimal volume)
    {
        if (week is null) return null;

        return new AnalysisWeekComparison(
            week.WeekNumber,
            week.StartDate,
            week.EndDate,
            week.TotalDays,
            week.CompletedDays,
            Percent(week.CompletedDays, week.TotalDays),
            week.TotalExercises,
            week.CompletedExercises,
            Math.Round(volume, 1)
        );
    }

    private static int Percent(decimal value, decimal total) =>
        total <= 0 ? 0 : (int)Math.Round(value / total * 100m, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Aggregates the user's training + body data into a compact snapshot the AI can reason over.
    /// Keep this small: only what's relevant for adherence, body trend, volume, and a recommendation.
    /// </summary>
    private async Task<Snapshot> BuildSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Active plan + current/recent weeks
        var activePlan = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
            })
            .FirstOrDefaultAsync(ct);

        PlanSnapshot? planSnap = null;
        WeekSnapshot? currentWeek = null;
        WeekSnapshot? previousWeek = null;

        if (activePlan is not null)
        {
            var weeks = await db.WeeklyWorkouts
                .AsNoTracking()
                .Where(w => w.PlanId == activePlan.Id)
                .OrderBy(w => w.WeekNumber)
                .Select(w => new
                {
                    w.Id,
                    w.WeekNumber,
                    w.StartDate,
                    w.EndDate,
                    Days = w.DailyWorkouts.Select(d => new
                    {
                        d.Id,
                        d.Date,
                        d.IsCompleted,
                        Total = d.Exercises.Count(),
                        Completed = d.Exercises.Count(e => e.IsCompleted),
                    }).ToList()
                })
                .ToListAsync(ct);

            int totalDays = weeks.Sum(w => w.Days.Count);
            int completedDays = weeks.Sum(w => w.Days.Count(d => d.IsCompleted));

            planSnap = new PlanSnapshot(
                activePlan.Name,
                activePlan.StartDate,
                activePlan.EndDate,
                totalDays,
                completedDays
            );

            var current = weeks.FirstOrDefault(w => w.StartDate <= today && today <= w.EndDate);
            current ??= weeks.LastOrDefault(w => w.StartDate <= today);

            if (current is not null)
            {
                currentWeek = new WeekSnapshot(
                    current.WeekNumber,
                    current.StartDate,
                    current.EndDate,
                    current.Days.Count,
                    current.Days.Count(d => d.IsCompleted),
                    current.Days.Sum(d => d.Total),
                    current.Days.Sum(d => d.Completed)
                );

                var prev = weeks.LastOrDefault(w => w.WeekNumber == current.WeekNumber - 1);
                if (prev is not null)
                {
                    previousWeek = new WeekSnapshot(
                        prev.WeekNumber,
                        prev.StartDate,
                        prev.EndDate,
                        prev.Days.Count,
                        prev.Days.Count(d => d.IsCompleted),
                        prev.Days.Sum(d => d.Total),
                        prev.Days.Sum(d => d.Completed)
                    );
                }
            }
        }

        // Bodyweight: last up to 8 entries
        var bw = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Date)
            .Take(8)
            .Select(b => new BodyweightPoint(b.Date, b.Weight))
            .ToListAsync(ct);

        // Recent volume and effort pattern (last 28 days completed sets)
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-28));
        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since)
            .Select(s => new
            {
                s.ActualReps,
                s.PlannedReps,
                s.ActualWeight,
                s.PlannedWeight,
                s.Rpe,
                Exercise = s.Exercise.Name,
                Muscle = s.Exercise.PrimaryMuscleGroup,
                Day = s.Exercise.DailyWorkout.Date
            })
            .ToListAsync(ct);

        decimal totalVolume = 0m;
        foreach (var s in recentSets)
        {
            var reps = s.ActualReps ?? s.PlannedReps;
            var w = s.ActualWeight ?? s.PlannedWeight ?? 0m;
            totalVolume += reps * w;
        }

        var byMuscle = recentSets
            .GroupBy(s => s.Muscle.ToString())
            .Select(g => new MuscleVolume(
                g.Key,
                g.Count(),
                g.Sum(x => (x.ActualReps ?? x.PlannedReps) * (x.ActualWeight ?? x.PlannedWeight ?? 0m))
            ))
            .OrderByDescending(m => m.Volume)
            .Take(10)
            .ToList();

        var highRpeMisses = recentSets
            .Where(s => s.Rpe >= 8.5m &&
                        ((s.ActualReps.HasValue && s.ActualReps.Value < s.PlannedReps) ||
                         (s.ActualWeight.HasValue && s.PlannedWeight.HasValue && s.ActualWeight.Value < s.PlannedWeight.Value)))
            .GroupBy(s => s.Exercise)
            .Select(g => new EffortGapEntry(
                g.Key,
                g.Count(),
                Math.Round(g.Average(x => x.Rpe ?? 0m), 1),
                "HighRpeMiss"
            ))
            .OrderByDescending(x => x.Sets)
            .ThenByDescending(x => x.AverageRpe)
            .Take(5)
            .ToList();

        var lowRpeWins = recentSets
            .Where(s => s.Rpe is <= 7.5m &&
                        (!s.ActualReps.HasValue || s.ActualReps.Value >= s.PlannedReps) &&
                        (!s.ActualWeight.HasValue || !s.PlannedWeight.HasValue || s.ActualWeight.Value >= s.PlannedWeight.Value))
            .GroupBy(s => s.Exercise)
            .Select(g => new EffortGapEntry(
                g.Key,
                g.Count(),
                Math.Round(g.Average(x => x.Rpe ?? 0m), 1),
                "LowRpeWin"
            ))
            .OrderByDescending(x => x.Sets)
            .ThenBy(x => x.AverageRpe)
            .Take(5)
            .ToList();

        var currentWeekVolume = currentWeek is null
            ? 0m
            : recentSets
                .Where(s => currentWeek.StartDate <= s.Day && s.Day <= currentWeek.EndDate)
                .Sum(s => (s.ActualReps ?? s.PlannedReps) * (s.ActualWeight ?? s.PlannedWeight ?? 0m));

        var previousWeekVolume = previousWeek is null
            ? 0m
            : recentSets
                .Where(s => previousWeek.StartDate <= s.Day && s.Day <= previousWeek.EndDate)
                .Sum(s => (s.ActualReps ?? s.PlannedReps) * (s.ActualWeight ?? s.PlannedWeight ?? 0m));

        // Top 5 PRs (most recently achieved)
        var prs = await (
            from p in db.UserExercisePRs.AsNoTracking()
            where p.UserId == userId
            join t in db.ExerciseTemplates.AsNoTracking() on p.ExerciseTemplateId equals t.Id
            orderby p.AchievedAt descending
            select new PrEntry(t.Name, p.Weight, p.Reps, p.AchievedAt)
        ).Take(5).ToListAsync(ct);

        // Streak
        var workoutDates = await db.WorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.Date)
            .Select(h => h.Date)
            .Take(30)
            .ToListAsync(ct);

        return new Snapshot(
            today,
            new ProfileContext(
                user.Gender?.ToString(),
                user.DateOfBirth,
                user.Height,
                user.DevelopmentDirection?.ToString(),
                user.TrainingDiscipline?.ToString()
            ),
            bw,
            workoutDates.Count,
            planSnap,
            currentWeek,
            previousWeek,
            Math.Round(totalVolume, 1),
            recentSets.Count,
            Math.Round(currentWeekVolume, 1),
            Math.Round(previousWeekVolume, 1),
            byMuscle,
            highRpeMisses,
            lowRpeWins,
            prs
        );
    }

    private static string ComputeFingerprint(Snapshot snapshot, string language)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            PromptVersion = AnalysisPromptVersion,
            language,
            snapshot.AsOf,
            snapshot.ProfileContext,
            snapshot.Plan,
            snapshot.CurrentWeek,
            snapshot.PreviousWeek,
            snapshot.RecentVolume,
            snapshot.RecentSetCount,
            BwSig = snapshot.RecentBodyweight.Select(b => $"{b.Date:O}:{b.Weight}"),
            MuscleSig = snapshot.MuscleVolumes.Select(m => $"{m.Muscle}:{m.Sets}:{m.Volume}"),
            HighRpeMissSig = snapshot.HighRpeMisses.Select(e => $"{e.Exercise}:{e.Sets}:{e.AverageRpe}"),
            LowRpeWinSig = snapshot.LowRpeWins.Select(e => $"{e.Exercise}:{e.Sets}:{e.AverageRpe}"),
            PrSig = snapshot.RecentPrs.Select(p => $"{p.Exercise}:{p.Weight}:{p.Reps}:{p.AchievedAt:O}"),
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    // ─── Snapshot record types (private to handler) ────────────────────────────

    private sealed record Snapshot(
        DateOnly AsOf,
        ProfileContext ProfileContext,
        IReadOnlyList<BodyweightPoint> RecentBodyweight,
        int Last30DaysWorkoutDates,
        PlanSnapshot? Plan,
        WeekSnapshot? CurrentWeek,
        WeekSnapshot? PreviousWeek,
        decimal RecentVolume,
        int RecentSetCount,
        decimal CurrentWeekVolume,
        decimal PreviousWeekVolume,
        IReadOnlyList<MuscleVolume> MuscleVolumes,
        IReadOnlyList<EffortGapEntry> HighRpeMisses,
        IReadOnlyList<EffortGapEntry> LowRpeWins,
        IReadOnlyList<PrEntry> RecentPrs
    );

    private sealed record ProfileContext(
        string? Gender,
        DateOnly? DateOfBirth,
        decimal? HeightCm,
        string? DevelopmentDirection,
        string? TrainingDiscipline
    );

    private sealed record PlanSnapshot(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        int TotalDays,
        int CompletedDays
    );

    private sealed record WeekSnapshot(
        int WeekNumber,
        DateOnly StartDate,
        DateOnly EndDate,
        int TotalDays,
        int CompletedDays,
        int TotalExercises,
        int CompletedExercises
    );

    private sealed record BodyweightPoint(DateOnly Date, decimal Weight);

    private sealed record MuscleVolume(string Muscle, int Sets, decimal Volume);

    private sealed record EffortGapEntry(string Exercise, int Sets, decimal AverageRpe, string Pattern);

    private sealed record PrEntry(string Exercise, decimal Weight, int Reps, DateTime AchievedAt);
}
