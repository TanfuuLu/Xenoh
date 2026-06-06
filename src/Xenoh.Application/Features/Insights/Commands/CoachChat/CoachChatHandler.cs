using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Insights.Commands.CoachChat;

public sealed class CoachChatHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<CoachChatCommand, CoachChatResponse>
{
    private const int MaxMessages = 30;
    private const int MaxMessageLength = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<CoachChatResponse> Handle(CoachChatCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var messages = (request.Messages ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(MaxMessages)
            .Select(m => new CoachChatAiMessage(
                m.Role == "assistant" ? "assistant" : "user",
                m.Content.Trim().Length > MaxMessageLength ? m.Content.Trim()[..MaxMessageLength] : m.Content.Trim()))
            .ToList();

        if (messages.Count == 0 || messages[^1].Role != "user")
            throw new InvalidOperationException("The last message must be from the user.");

        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
        var snapshot = await BuildContextAsync(userId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        var result = await ai.ChatAsync(
            new CoachChatAiRequest(language, snapshotJson, messages),
            cancellationToken);

        return new CoachChatResponse(result.Reply);
    }

    private async Task<object> BuildContextAsync(Guid userId, CancellationToken ct)
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
                DevelopmentDirection = u.DevelopmentDirection.HasValue ? u.DevelopmentDirection.Value.ToString() : null,
                TrainingDiscipline = u.TrainingDiscipline.HasValue ? u.TrainingDiscipline.Value.ToString() : null,
            })
            .FirstOrDefaultAsync(ct);

        var activePlan = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.IsActive)
            .Select(p => new
            {
                p.Name,
                p.StartDate,
                p.EndDate,
                TotalDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.Exercises.Any()),
                CompletedDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts)
                    .Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted)),
            })
            .FirstOrDefaultAsync(ct);

        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since)
            .Select(s => new
            {
                s.Exercise.PrimaryMuscleGroup,
                s.PlannedReps,
                s.PlannedWeight,
                s.ActualReps,
                s.ActualWeight,
                s.Rpe,
            })
            .ToListAsync(ct);

        var topMuscles = recentSets
            .GroupBy(s => s.PrimaryMuscleGroup.ToString())
            .Select(g => new { Muscle = g.Key, Sets = g.Count() })
            .OrderByDescending(m => m.Sets)
            .Take(6)
            .ToList();

        var latestBodyweight = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(ct);

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Join(db.ExerciseTemplates.AsNoTracking(),
                p => p.ExerciseTemplateId, t => t.Id,
                (p, t) => new { Exercise = t.Name, p.Weight, p.Reps, p.AchievedAt })
            .OrderByDescending(p => p.AchievedAt)
            .Take(5)
            .ToListAsync(ct);

        var nutrition = await db.NutritionDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= since)
            .ToListAsync(ct);

        return new
        {
            AsOf = today,
            Profile = profile,
            LatestBodyweightKg = latestBodyweight,
            ActivePlan = activePlan is null ? null : new
            {
                activePlan.Name,
                activePlan.StartDate,
                activePlan.EndDate,
                activePlan.TotalDays,
                activePlan.CompletedDays,
                CompletionPercent = activePlan.TotalDays == 0
                    ? 0
                    : (int)Math.Round(activePlan.CompletedDays * 100.0 / activePlan.TotalDays),
            },
            Recent28Days = new
            {
                CompletedSets = recentSets.Count,
                AverageRpe = recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
                HighRpeSets = recentSets.Count(s => s.Rpe >= 8.5m),
                MissedTargetSets = recentSets.Count(s =>
                    (s.ActualReps is not null && s.ActualReps < s.PlannedReps) ||
                    (s.ActualWeight is not null && s.PlannedWeight is not null && s.ActualWeight < s.PlannedWeight)),
                TopMuscles = topMuscles,
            },
            Nutrition = nutrition.Count == 0 ? null : new
            {
                LoggedDays = nutrition.Count,
                AverageCalories = (int)Math.Round(nutrition.Average(l => l.Calories)),
                AverageProteinG = Math.Round(nutrition.Average(l => l.ProteinG), 1),
            },
            RecentPrs = prs,
        };
    }
}
