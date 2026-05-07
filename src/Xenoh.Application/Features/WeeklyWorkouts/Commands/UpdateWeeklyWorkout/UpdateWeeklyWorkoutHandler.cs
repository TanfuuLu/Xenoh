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

        int effectiveDays = week.DailyWorkouts.Count(d => d.Date >= plan.StartDate && d.Date <= plan.EndDate);
        return new WeeklyWorkoutResponse(
            week.Id,
            week.WeekNumber,
            week.Name,
            week.StartDate,
            week.EndDate,
            week.PlanId,
            effectiveDays,
            week.DailyWorkouts.Count(d =>
                d.Date >= plan.StartDate && d.Date <= plan.EndDate &&
                (d.IsCompleted || d.Status == DayStatus.Rest || d.Status == DayStatus.Missed)),
            week.DailyWorkouts.Any(d => d.Exercises.Any(e => e.Sets.Any(s =>
                s.IsCompleted &&
                ((s.ActualReps != null && s.ActualReps < s.PlannedReps) ||
                 (s.ActualWeight != null && s.PlannedWeight != null && s.ActualWeight < s.PlannedWeight))))),
            week.IsCompleted,
            effectiveDays
        );
    }
}
