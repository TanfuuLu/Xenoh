using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.DeleteExercise;

public sealed class DeleteExerciseHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteExerciseCommand>
{
    public async ValueTask<Unit> Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var exercise = await context.Exercises
            .Include(e => e.DailyWorkout)
                .ThenInclude(d => d.WeeklyWorkout)
                    .ThenInclude(w => w.Plan)
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId, cancellationToken)
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

        context.Exercises.Remove(exercise);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
