using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.DuplicatePlan;

public sealed class DuplicatePlanHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<DuplicatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(DuplicatePlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var userId = currentUser.UserId;

        var source = await planRepo.GetForDuplicateAsync(request.SourcePlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found or access denied.");

        var maxPlans = await subscriptionService.GetMaxPlansAsync(userId, cancellationToken);
        var planCount = await planRepo.CountByOwnerAsync(userId, cancellationToken);
        if (maxPlans != int.MaxValue && planCount >= maxPlans)
            throw new InvalidOperationException(
                $"Your current plan allows a maximum of {maxPlans} plans. Upgrade to Pro for unlimited plans.");

        var newPlan = new Plan
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanType = PlanType.Self,
            OwnerId = userId,
            IsActive = planCount == 0
        };

        newPlan.WeeklyWorkouts = PlanWeekGenerator.Generate(newPlan);

        // Build lookup: (weekNumber, dayOfWeek) → source exercises
        var sourceExerciseMap = source.WeeklyWorkouts
            .SelectMany(w => w.DailyWorkouts.Select(d => (w.WeekNumber, d.DayOfWeek, d.Exercises)))
            .ToDictionary(x => (x.WeekNumber, x.DayOfWeek), x => x.Exercises);

        foreach (var week in newPlan.WeeklyWorkouts)
        {
            foreach (var day in week.DailyWorkouts)
            {
                if (!sourceExerciseMap.TryGetValue((week.WeekNumber, day.DayOfWeek), out var sourceExercises))
                    continue;

                day.Exercises = sourceExercises.Select(se => new Exercise
                {
                    ExerciseTemplateId = se.ExerciseTemplateId,
                    Name = se.Name,
                    PrimaryMuscleGroup = se.PrimaryMuscleGroup,
                    SecondaryMuscleGroups = [.. se.SecondaryMuscleGroups],
                    ExerciseKind = se.ExerciseKind,
                    EstimatedMet = se.EstimatedMet,
                    PlannedSets = se.PlannedSets,
                    PlannedReps = se.PlannedReps,
                    PlannedWeight = se.PlannedWeight,
                    Notes = se.Notes,
                    SortOrder = se.SortOrder,
                    IsCompleted = false,
                    XpAwarded = false,
                    Sets = Enumerable.Range(1, se.PlannedSets).Select(i => new ExerciseSet
                    {
                        SetNumber = i,
                        PlannedReps = se.Sets.ElementAtOrDefault(i - 1)?.PlannedReps ?? se.PlannedReps,
                        PlannedWeight = se.Sets.ElementAtOrDefault(i - 1)?.PlannedWeight ?? se.PlannedWeight,
                        IsCompleted = false
                    }).ToList<ExerciseSet>()
                }).ToList<Exercise>();
            }
        }

        await planRepo.AddAsync(newPlan, cancellationToken);
        await planRepo.SaveChangesAsync(cancellationToken);

        return await planRepo.GetByIdForUserAsync(newPlan.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload duplicated plan.");
    }
}
