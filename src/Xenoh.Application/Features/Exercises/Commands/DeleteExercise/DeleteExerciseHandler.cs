using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.DeleteExercise;

public sealed class DeleteExerciseHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteExerciseCommand>
{
    public async ValueTask<Unit> Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var exercise = await exerciseRepo.FindWithPlanAsync(request.ExerciseId, cancellationToken)
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

        var dailyWorkout = exercise.DailyWorkout;
        var remainingExercises = dailyWorkout.Exercises.Where(e => e.Id != exercise.Id).ToList();

        dailyWorkout.IsCompleted = remainingExercises.Count > 0 && remainingExercises.All(e => e.IsCompleted);
        dailyWorkout.UpdatedAt = DateTime.UtcNow;

        exerciseRepo.Remove(exercise);
        await exerciseRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
