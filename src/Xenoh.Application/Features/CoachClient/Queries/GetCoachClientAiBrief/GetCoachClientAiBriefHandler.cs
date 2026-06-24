using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachClientAiBrief;

public sealed class GetCoachClientAiBriefHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<GetCoachClientAiBriefQuery, CoachClientAiBriefResponse>
{
    private const string FeatureName = "coach-client-ai-brief";
    private const int PromptVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<CoachClientAiBriefResponse> Handle(
        GetCoachClientAiBriefQuery request,
        CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        if (coachId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var language = NormalizeLanguage(request.Language);
        var snapshot = await BuildSnapshotAsync(coachId, request.ClientId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var fingerprint = ComputeFingerprint(snapshotJson, language);

        var cache = await db.AiFeatureCaches.FirstOrDefaultAsync(c =>
            c.Feature == FeatureName &&
            c.Language == language &&
            c.UserId == coachId &&
            c.SubjectUserId == request.ClientId &&
            c.ResourceId == null,
            cancellationToken);

        if (cache is not null && cache.DataFingerprint == fingerprint)
            return ToResponse(language, cache.GeneratedAt, true, cache.ContentJson);

        var aiResult = await ai.GenerateCoachClientBriefAsync(
            new CoachClientBriefAiRequest(language, snapshotJson),
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
                UserId = coachId,
                SubjectUserId = request.ClientId,
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

    private async Task<object> BuildSnapshotAsync(Guid coachId, Guid clientId, CancellationToken ct)
    {
        var relationship = await db.CoachClientRelationships
            .AsNoTracking()
            .Where(r => r.CoachId == coachId &&
                        r.ClientId == clientId &&
                        (r.Status == RelationshipStatus.Active || r.Status == RelationshipStatus.PendingRenewal))
            .Select(r => new
            {
                r.ClientId,
                ClientName = (r.Client.FirstName + " " + r.Client.LastName).Trim(),
                r.Client.Email,
                r.Client.Gender,
                r.Client.DateOfBirth,
                r.Client.Height,
                r.Client.DevelopmentDirection,
                r.Client.TrainingDiscipline,
                r.Status,
                r.StartDate,
                r.EndDate,
                r.ProposedEndDate
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activePlan = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == clientId && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                TotalWorkoutDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(),
                CompletedWorkoutDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.IsCompleted),
                MissedWorkoutDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.Status == DayStatus.Missed),
                UpcomingDays = p.WeeklyWorkouts
                    .SelectMany(w => w.DailyWorkouts)
                    .Where(d => d.Date >= today)
                    .OrderBy(d => d.Date)
                    .Take(7)
                    .Select(d => new
                    {
                        d.Date,
                        d.DayOfWeek,
                        d.Status,
                        ExerciseCount = d.Exercises.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        var lastWorkoutDate = await db.WorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == clientId)
            .OrderByDescending(h => h.Date)
            .Select(h => (DateOnly?)h.Date)
            .FirstOrDefaultAsync(ct);

        int? daysSinceLastWorkout = lastWorkoutDate is null
            ? null
            : today.DayNumber - lastWorkoutDate.Value.DayNumber;

        var monitoring = activePlan is null
            ? null
            : new CoachPlanMonitoringSnapshot(
                clientId,
                activePlan.Id,
                activePlan.Name,
                activePlan.StartDate,
                activePlan.EndDate,
                activePlan.TotalWorkoutDays > 0
                    ? (int?)Math.Round(activePlan.CompletedWorkoutDays * 100.0 / activePlan.TotalWorkoutDays)
                    : null,
                activePlan.MissedWorkoutDays,
                activePlan.CompletedWorkoutDays,
                activePlan.TotalWorkoutDays,
                CalculateExpectedProgressPercent(activePlan.StartDate, activePlan.EndDate, today));

        var attention = CoachAttentionAnalyzer.Analyze(monitoring, daysSinceLastWorkout);

        var bodyweight = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == clientId)
            .OrderByDescending(b => b.Date)
            .Take(8)
            .Select(b => new { b.Date, b.Weight })
            .ToListAsync(ct);

        var recentSetsSince = today.AddDays(-28);
        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == clientId &&
                        s.Exercise.DailyWorkout.Date >= recentSetsSince)
            .Select(s => new
            {
                Date = s.Exercise.DailyWorkout.Date,
                Exercise = s.Exercise.Name,
                Muscle = s.Exercise.PrimaryMuscleGroup.ToString(),
                s.PlannedReps,
                s.PlannedWeight,
                s.ActualReps,
                s.ActualWeight,
                s.Rpe
            })
            .ToListAsync(ct);

        var recentMuscleVolume = recentSets
            .GroupBy(s => s.Muscle)
            .Select(g => new
            {
                Muscle = g.Key,
                Sets = g.Count(),
                Volume = g.Sum(x => (x.ActualReps ?? x.PlannedReps) * (x.ActualWeight ?? x.PlannedWeight ?? 0m))
            })
            .OrderByDescending(x => x.Volume)
            .Take(8)
            .ToList();

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == clientId)
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
            Relationship = relationship,
            ClientProfile = new
            {
                Gender = relationship.Gender.HasValue ? relationship.Gender.Value.ToString() : null,
                relationship.DateOfBirth,
                HeightCm = relationship.Height,
                DevelopmentDirection = relationship.DevelopmentDirection.HasValue
                    ? relationship.DevelopmentDirection.Value.ToString()
                    : null,
                TrainingDiscipline = relationship.TrainingDiscipline.HasValue
                    ? relationship.TrainingDiscipline.Value.ToString()
                    : null
            },
            Attention = new { attention.Level, attention.Reasons },
            LastWorkoutDate = lastWorkoutDate,
            DaysSinceLastWorkout = daysSinceLastWorkout,
            ActivePlan = activePlan,
            RecentBodyweight = bodyweight,
            Recent28Days = new
            {
                CompletedSets = recentSets.Count,
                AverageRpe = recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
                MuscleVolume = recentMuscleVolume
            },
            RecentPrs = prs
        };
    }

    private static CoachClientAiBriefContent ParseContent(string json)
    {
        var content = JsonSerializer.Deserialize<CoachClientAiBriefContent>(json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        return content with
        {
            AttentionLevel = NormalizeAttentionLevel(content.AttentionLevel),
            Risks = content.Risks.Take(5).ToList(),
            Opportunities = content.Opportunities.Take(5).ToList(),
            SuggestedMessage = content.SuggestedMessage.Length <= 400
                ? content.SuggestedMessage
                : content.SuggestedMessage[..400],
        };
    }

    private static CoachClientAiBriefResponse ToResponse(string language, DateTime generatedAt, bool cached, string json)
    {
        var content = ParseContent(json);
        return new CoachClientAiBriefResponse(
            language,
            generatedAt,
            cached,
            content.Headline,
            content.AttentionLevel,
            content.ProgressSummary,
            content.Risks,
            content.Opportunities,
            content.SuggestedMessage);
    }

    private static int CalculateExpectedProgressPercent(DateOnly startDate, DateOnly endDate, DateOnly today)
    {
        var totalDays = Math.Max(1, endDate.DayNumber - startDate.DayNumber + 1);
        var elapsedDays = Math.Clamp(today.DayNumber - startDate.DayNumber + 1, 0, totalDays);
        return (int)Math.Round(elapsedDays * 100.0 / totalDays);
    }

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

    private static string NormalizeAttentionLevel(string attentionLevel) =>
        string.Equals(attentionLevel, "High", StringComparison.OrdinalIgnoreCase) ? "High"
        : string.Equals(attentionLevel, "Medium", StringComparison.OrdinalIgnoreCase) ? "Medium"
        : string.Equals(attentionLevel, "Low", StringComparison.OrdinalIgnoreCase) ? "Low"
        : "None";

    private static string ComputeFingerprint(string snapshotJson, string language)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{FeatureName}:v{PromptVersion}:{language}:{snapshotJson}"));
        return Convert.ToHexString(hash);
    }

    private sealed record CoachClientAiBriefContent(
        string Headline,
        string AttentionLevel,
        string ProgressSummary,
        IReadOnlyList<string> Risks,
        IReadOnlyList<string> Opportunities,
        string SuggestedMessage
    );
}
