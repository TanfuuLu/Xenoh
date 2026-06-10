using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Plans.Queries.ReviewPlanBalance;

public sealed class ReviewPlanBalanceHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<ReviewPlanBalanceQuery, PlanBalanceReviewResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<PlanBalanceReviewResponse> Handle(
        ReviewPlanBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

        var plan = await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == request.PlanId &&
                        (p.OwnerId == userId || p.CreatedByCoachId == userId))
            .Select(p => new
            {
                p.OwnerId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.PlanType,
                OwnerProfile = new
                {
                    Gender = p.Owner.Gender.HasValue ? p.Owner.Gender.Value.ToString() : null,
                    p.Owner.DateOfBirth,
                    HeightCm = p.Owner.Height,
                    DevelopmentDirection = p.Owner.DevelopmentDirection.HasValue
                        ? p.Owner.DevelopmentDirection.Value.ToString()
                        : null,
                    TrainingDiscipline = p.Owner.TrainingDiscipline.HasValue
                        ? p.Owner.TrainingDiscipline.Value.ToString()
                        : null
                },
                Weeks = p.WeeklyWorkouts
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new
                    {
                        w.WeekNumber,
                        Days = w.DailyWorkouts
                            .OrderBy(d => d.Date)
                            .Select(d => new
                            {
                                d.Date,
                                d.DayOfWeek,
                                Exercises = d.Exercises
                                    .OrderBy(e => e.SortOrder)
                                    .Select(e => new
                                    {
                                        e.Name,
                                        Muscle = e.PrimaryMuscleGroup.ToString(),
                                        SecondaryMuscles = e.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
                                        Kind = e.ExerciseKind.ToString(),
                                        e.PlannedSets,
                                        e.PlannedReps,
                                        e.PlannedWeight
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var exercises = plan.Weeks.SelectMany(w => w.Days).SelectMany(d => d.Exercises).ToList();
        var muscleTotals = exercises
            .GroupBy(e => e.Muscle)
            .Select(g => new
            {
                Muscle = g.Key,
                Sets = g.Sum(e => e.PlannedSets),
                Exercises = g.Count()
            })
            .OrderByDescending(x => x.Sets)
            .ToList();

        // Only surface the owner's cycle data on self-review. A coach reviewing a client's
        // plan must not see menstrual/pre-menstrual dates (that data is gated by the separate
        // ShareWithCoach opt-in), so we omit cycle context unless the requester is the owner.
        var cycleContext = plan.OwnerId == userId
            ? await CycleContextBuilder.TryBuildAsync(db, userId, plan.StartDate, plan.EndDate, cancellationToken)
            : null;

        var snapshot = new
        {
            plan.Name,
            plan.StartDate,
            plan.EndDate,
            PlanType = plan.PlanType.ToString(),
            ProfileContext = plan.OwnerProfile,
            CycleContext = cycleContext,
            TotalExercises = exercises.Count,
            MuscleTotals = muscleTotals,
            Weeks = plan.Weeks
        };

        var aiResult = await ai.ReviewPlanBalanceAsync(
            new PlanBalanceAiRequest(language, JsonSerializer.Serialize(snapshot, JsonOptions)),
            cancellationToken);

        var result = JsonSerializer.Deserialize<PlanBalanceReviewResponse>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        return result with
        {
            Severity = NormalizeSeverity(result.Severity),
            Warnings = result.Warnings.Take(5).ToList(),
            Suggestions = result.Suggestions.Take(5).ToList()
        };
    }

    private static string NormalizeSeverity(string severity)
    {
        return string.Equals(severity, "High", StringComparison.OrdinalIgnoreCase) ? "High"
            : string.Equals(severity, "Medium", StringComparison.OrdinalIgnoreCase) ? "Medium"
            : "Low";
    }
}
