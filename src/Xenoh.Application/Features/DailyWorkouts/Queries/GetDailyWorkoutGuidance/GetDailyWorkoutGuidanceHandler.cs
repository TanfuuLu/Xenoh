using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDailyWorkoutGuidance;

public sealed class GetDailyWorkoutGuidanceHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<GetDailyWorkoutGuidanceQuery, WorkoutGuidanceResponse>
{
    private const string FeatureName = "daily-workout-guidance";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<WorkoutGuidanceResponse> Handle(
        GetDailyWorkoutGuidanceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var language = NormalizeLanguage(request.Language);
        var snapshot = await BuildSnapshotAsync(request.DailyWorkoutId, userId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var fingerprint = ComputeFingerprint(snapshotJson, language);

        var cache = await db.AiFeatureCaches.FirstOrDefaultAsync(c =>
            c.Feature == FeatureName &&
            c.Language == language &&
            c.UserId == userId &&
            c.SubjectUserId == null &&
            c.ResourceId == request.DailyWorkoutId,
            cancellationToken);

        if (cache is not null && cache.DataFingerprint == fingerprint)
            return ToResponse(language, cache.GeneratedAt, true, cache.ContentJson);

        var aiResult = await ai.GenerateWorkoutGuidanceAsync(
            new WorkoutGuidanceAiRequest(language, snapshotJson),
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
                ResourceId = request.DailyWorkoutId,
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

    private async Task<object> BuildSnapshotAsync(Guid dailyWorkoutId, Guid userId, CancellationToken ct)
    {
        var dayExists = await db.DailyWorkouts
            .AsNoTracking()
            .AnyAsync(d => d.Id == dailyWorkoutId, ct);

        var day = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d => d.Id == dailyWorkoutId && d.WeeklyWorkout.Plan.OwnerId == userId)
            .Select(d => new
            {
                d.Id,
                d.Date,
                d.DayOfWeek,
                d.Status,
                d.IsCompleted,
                Plan = new
                {
                    d.WeeklyWorkout.Plan.Name,
                    d.WeeklyWorkout.Plan.StartDate,
                    d.WeeklyWorkout.Plan.EndDate,
                    d.WeeklyWorkout.Plan.IsActive,
                    d.WeeklyWorkout.WeekNumber
                },
                Exercises = d.Exercises
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new
                    {
                        e.Id,
                        e.ExerciseTemplateId,
                        e.Name,
                        Muscle = e.PrimaryMuscleGroup.ToString(),
                        SecondaryMuscles = e.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
                        Kind = e.ExerciseKind.ToString(),
                        e.PlannedSets,
                        e.PlannedReps,
                        e.PlannedWeight,
                        e.IsCompleted,
                        CompletedSets = e.Sets.Count(s => s.IsCompleted),
                        Sets = e.Sets
                            .OrderBy(s => s.SetNumber)
                            .Select(s => new
                            {
                                s.SetNumber,
                                s.PlannedReps,
                                s.PlannedWeight,
                                s.ActualReps,
                                s.ActualWeight,
                                s.Rpe,
                                s.IsCompleted
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw (dayExists
                ? new UnauthorizedAccessException()
                : new InvalidOperationException("Daily workout not found."));

        var templateIds = day.Exercises.Select(e => e.ExerciseTemplateId).Distinct().ToList();
        var since = day.Date.AddDays(-28);

        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since &&
                        s.Exercise.DailyWorkout.Date < day.Date)
            .Select(s => new
            {
                Date = s.Exercise.DailyWorkout.Date,
                Exercise = s.Exercise.Name,
                s.Exercise.ExerciseTemplateId,
                s.PlannedReps,
                s.PlannedWeight,
                s.ActualReps,
                s.ActualWeight,
                s.Rpe
            })
            .ToListAsync(ct);

        var recentTemplatePerformance = recentSets
            .Where(s => templateIds.Contains(s.ExerciseTemplateId))
            .GroupBy(s => s.ExerciseTemplateId)
            .Select(g => new
            {
                ExerciseTemplateId = g.Key,
                LastDate = g.Max(x => x.Date),
                CompletedSets = g.Count(),
                AverageRpe = g.Where(x => x.Rpe.HasValue).Select(x => x.Rpe!.Value).DefaultIfEmpty().Average(),
                TopWeight = g.Max(x => x.ActualWeight ?? x.PlannedWeight ?? 0m)
            })
            .ToList();

        var highRpeMisses = recentSets
            .Where(s => s.Rpe >= 8.5m &&
                        ((s.ActualReps.HasValue && s.ActualReps.Value < s.PlannedReps) ||
                         (s.ActualWeight.HasValue && s.PlannedWeight.HasValue && s.ActualWeight.Value < s.PlannedWeight.Value)))
            .GroupBy(s => s.Exercise)
            .Select(g => new { Exercise = g.Key, Sets = g.Count(), AverageRpe = Math.Round(g.Average(x => x.Rpe ?? 0m), 1) })
            .OrderByDescending(x => x.Sets)
            .Take(5)
            .ToList();

        var bodyweight = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Date <= day.Date)
            .OrderByDescending(b => b.Date)
            .Take(8)
            .Select(b => new { b.Date, b.Weight })
            .ToListAsync(ct);

        return new
        {
            AsOf = DateOnly.FromDateTime(DateTime.UtcNow),
            Workout = day,
            Recent28Days = new
            {
                CompletedSets = recentSets.Count,
                AverageRpe = recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
                HighRpeMisses = highRpeMisses,
                TemplatePerformance = recentTemplatePerformance
            },
            RecentBodyweight = bodyweight
        };
    }

    private static WorkoutGuidanceContent ParseContent(string json)
    {
        var content = JsonSerializer.Deserialize<WorkoutGuidanceContent>(json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        return content with
        {
            Readiness = NormalizeReadiness(content.Readiness),
            RecommendedAdjustments = content.RecommendedAdjustments.Take(5).ToList(),
            CautionFlags = content.CautionFlags.Take(5).ToList(),
            NextBestActions = content.NextBestActions.Take(5).ToList(),
        };
    }

    private static WorkoutGuidanceResponse ToResponse(string language, DateTime generatedAt, bool cached, string json)
    {
        var content = ParseContent(json);
        return new WorkoutGuidanceResponse(
            language,
            generatedAt,
            cached,
            content.Headline,
            content.Readiness,
            content.RecommendedAdjustments,
            content.CautionFlags,
            content.NextBestActions);
    }

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

    private static string NormalizeReadiness(string readiness) =>
        string.Equals(readiness, "High", StringComparison.OrdinalIgnoreCase) ? "High"
        : string.Equals(readiness, "Low", StringComparison.OrdinalIgnoreCase) ? "Low"
        : "Moderate";

    private static string ComputeFingerprint(string snapshotJson, string language)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{language}:{snapshotJson}"));
        return Convert.ToHexString(hash);
    }

    private sealed record WorkoutGuidanceContent(
        string Headline,
        string Readiness,
        IReadOnlyList<string> RecommendedAdjustments,
        IReadOnlyList<string> CautionFlags,
        IReadOnlyList<string> NextBestActions
    );
}
