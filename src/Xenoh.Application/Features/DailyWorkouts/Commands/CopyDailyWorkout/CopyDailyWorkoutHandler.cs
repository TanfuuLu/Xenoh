using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.CopyDailyWorkout;

public sealed class CopyDailyWorkoutHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<CopyDailyWorkoutCommand, CopyDailyWorkoutResponse>
{
    public async ValueTask<CopyDailyWorkoutResponse> Handle(CopyDailyWorkoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (request.SourceDailyWorkoutId == request.TargetDailyWorkoutId)
            throw new InvalidOperationException("Source and target cannot be the same daily workout.");

        var source = await context.DailyWorkouts
            .Include(d => d.WeeklyWorkout).ThenInclude(w => w.Plan)
            .Include(d => d.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(d => d.Id == request.SourceDailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Source daily workout not found.");

        var sourcePlan = source.WeeklyWorkout.Plan;
        if (sourcePlan.OwnerId != userId && sourcePlan.CreatedByCoachId != userId)
            throw new InvalidOperationException("Access denied to source daily workout.");

        var target = await context.DailyWorkouts
            .Include(d => d.WeeklyWorkout).ThenInclude(w => w.Plan)
            .Include(d => d.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(d => d.Id == request.TargetDailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Target daily workout not found.");

        var targetPlan = target.WeeklyWorkout.Plan;
        bool canEditTarget = targetPlan.PlanType == PlanType.Coach
            ? targetPlan.CreatedByCoachId == userId
            : targetPlan.OwnerId == userId;

        if (!canEditTarget)
            throw new InvalidOperationException("Access denied to target daily workout.");

        // Clear existing exercises in target before copying
        context.Exercises.RemoveRange(target.Exercises);

        // Clone exercises (planned data only — actuals and completion state reset)
        var cloned = source.Exercises.Select(e => new Exercise
        {
            ExerciseTemplateId = e.ExerciseTemplateId,
            Name = e.Name,
            PrimaryMuscleGroup = e.PrimaryMuscleGroup,
            SecondaryMuscleGroups = [.. e.SecondaryMuscleGroups],
            PlannedSets = e.PlannedSets,
            PlannedReps = e.PlannedReps,
            PlannedWeight = e.PlannedWeight,
            Notes = e.Notes,
            DailyWorkoutId = target.Id,
            Sets = e.Sets.OrderBy(s => s.SetNumber).Select(s => new ExerciseSet
            {
                SetNumber = s.SetNumber,
                PlannedReps = s.PlannedReps,
                PlannedWeight = s.PlannedWeight
            }).ToList<ExerciseSet>()
        }).ToList();

        context.Exercises.AddRange(cloned);

        target.IsCompleted = false;
        target.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return new CopyDailyWorkoutResponse(target.Id, cloned.Count);
    }
}
