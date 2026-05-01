using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.CopyDailyWorkout;

public sealed class CopyDailyWorkoutHandler(
    IDailyWorkoutRepository dailyWorkoutRepo,
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<CopyDailyWorkoutCommand, CopyDailyWorkoutResponse>
{
    public async ValueTask<CopyDailyWorkoutResponse> Handle(
        CopyDailyWorkoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (request.SourceDailyWorkoutId == request.TargetDailyWorkoutId)
            throw new InvalidOperationException("Source and target cannot be the same daily workout.");

        var source = await dailyWorkoutRepo.FindWithExercisesAndPlanAsync(
            request.SourceDailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Source daily workout not found.");

        var sourcePlan = source.WeeklyWorkout.Plan;
        if (sourcePlan.OwnerId != userId && sourcePlan.CreatedByCoachId != userId)
            throw new InvalidOperationException("Access denied to source daily workout.");

        var target = await dailyWorkoutRepo.FindWithExercisesAndPlanAsync(
            request.TargetDailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Target daily workout not found.");

        var targetPlan = target.WeeklyWorkout.Plan;
        bool canEditTarget = targetPlan.PlanType == PlanType.Coach
            ? targetPlan.CreatedByCoachId == userId
            : targetPlan.OwnerId == userId;

        if (!canEditTarget)
            throw new InvalidOperationException("Access denied to target daily workout.");

        exerciseRepo.RemoveRange(target.Exercises);

        var cloned = source.Exercises
        .OrderBy(e => e.SortOrder)
        .ThenBy(e => e.CreatedAt)
        .Select((e, index) => new Exercise
        {
            ExerciseTemplateId = e.ExerciseTemplateId,
            Name = e.Name,
            PrimaryMuscleGroup = e.PrimaryMuscleGroup,
            SecondaryMuscleGroups = [.. e.SecondaryMuscleGroups],
            ExerciseKind = e.ExerciseKind,
            EstimatedMet = e.EstimatedMet,
            PlannedSets = e.PlannedSets,
            PlannedReps = e.PlannedReps,
            PlannedWeight = e.PlannedWeight,
            Notes = e.Notes,
            DailyWorkoutId = target.Id,
            SortOrder = index,
            Sets = e.Sets.OrderBy(s => s.SetNumber).Select(s => new ExerciseSet
            {
                SetNumber = s.SetNumber,
                PlannedReps = s.PlannedReps,
                PlannedWeight = s.PlannedWeight
            }).ToList<ExerciseSet>()
        }).ToList();

        exerciseRepo.AddRange(cloned);

        target.IsCompleted = false;
        target.UpdatedAt = DateTime.UtcNow;

        await dailyWorkoutRepo.SaveChangesAsync(cancellationToken);

        return new CopyDailyWorkoutResponse(target.Id, cloned.Count);
    }
}
