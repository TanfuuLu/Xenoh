using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.UpdateExercise;

public sealed class UpdateExerciseHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateExerciseCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException(
                plan.PlanType == PlanType.Coach && plan.OwnerId == userId
                    ? "This plan is managed by your coach and cannot be edited."
                    : "Access denied.");

        var plannedSetsChanged = request.PlannedSets is not null && request.PlannedSets.Value != exercise.PlannedSets;

        if (plannedSetsChanged)
        {
            bool hasCompletedSets = exercise.Sets.Any(s => s.IsCompleted);
            if (hasCompletedSets)
                throw new InvalidOperationException("Cannot change PlannedSets after sets have been completed.");

            exerciseRepo.RemoveSetRange(exercise.Sets);
            exercise.PlannedSets = request.PlannedSets!.Value;

            int reps = request.PlannedReps ?? exercise.PlannedReps;
            decimal? weight = request.PlannedWeight ?? exercise.PlannedWeight;

            for (int i = 1; i <= exercise.PlannedSets; i++)
            {
                exercise.Sets.Add(new ExerciseSet
                {
                    SetNumber = i,
                    PlannedReps = reps,
                    PlannedWeight = weight
                });
            }
        }

        if (request.PlannedReps is not null)
        {
            exercise.PlannedReps = request.PlannedReps.Value;
            if (!plannedSetsChanged)
            {
                foreach (var set in exercise.Sets.Where(s => !s.IsCompleted))
                    set.PlannedReps = request.PlannedReps.Value;
            }
        }

        if (request.PlannedWeight is not null)
        {
            exercise.PlannedWeight = request.PlannedWeight;
            if (!plannedSetsChanged)
            {
                foreach (var set in exercise.Sets.Where(s => !s.IsCompleted))
                    set.PlannedWeight = request.PlannedWeight;
            }
        }

        if (request.Notes is not null) exercise.Notes = request.Notes;

        exercise.UpdatedAt = DateTime.UtcNow;

        await exerciseRepo.SaveChangesAsync(cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise);
    }
}
