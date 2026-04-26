using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.WeeklyWorkouts.Commands.UpdateWeeklyWorkout;

public sealed class UpdateWeeklyWorkoutHandler(
    IWeeklyWorkoutRepository weeklyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateWeeklyWorkoutCommand, WeeklyWorkoutResponse>
{
    public async ValueTask<WeeklyWorkoutResponse> Handle(
        UpdateWeeklyWorkoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var week = await weeklyWorkoutRepo.FindForMutationAsync(request.WeeklyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Weekly workout not found.");

        var plan = week.Plan;
        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException(
                plan.PlanType == PlanType.Coach && plan.OwnerId == userId
                    ? "This plan is managed by your coach and cannot be edited."
                    : "Access denied.");

        week.Name = request.Name.Trim();
        week.UpdatedAt = DateTime.UtcNow;

        await weeklyWorkoutRepo.SaveChangesAsync(cancellationToken);

        return new WeeklyWorkoutResponse(
            week.Id,
            week.WeekNumber,
            week.Name,
            week.StartDate,
            week.EndDate,
            week.PlanId,
            week.DailyWorkouts.Count,
            week.DailyWorkouts.Count(d => d.IsCompleted)
        );
    }
}
