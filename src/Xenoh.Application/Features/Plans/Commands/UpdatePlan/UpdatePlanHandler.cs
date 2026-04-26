using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.UpdatePlan;

public sealed class UpdatePlanHandler(
    IPlanRepository planRepo,
    IWeeklyWorkoutRepository weeklyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var userId = currentUser.UserId;

        var plan = await planRepo.FindForMutationAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException("You do not have permission to update this plan.");

        bool datesChanged = plan.StartDate != request.StartDate || plan.EndDate != request.EndDate;

        if (datesChanged)
        {
            var hasProgress = plan.WeeklyWorkouts
                .SelectMany(w => w.DailyWorkouts)
                .Any(d => d.IsCompleted);

            if (hasProgress)
                throw new InvalidOperationException("Cannot change dates: plan already has completed days.");

            weeklyWorkoutRepo.RemoveRange(plan.WeeklyWorkouts);

            plan.StartDate = request.StartDate;
            plan.EndDate = request.EndDate;
            plan.Name = request.Name;

            await planRepo.SaveChangesAsync(cancellationToken);

            var newWeeks = PlanWeekGenerator.Generate(plan);
            weeklyWorkoutRepo.AddRange(newWeeks);
        }
        else
        {
            plan.Name = request.Name;
        }

        plan.UpdatedAt = DateTime.UtcNow;
        await planRepo.SaveChangesAsync(cancellationToken);

        return await planRepo.GetByIdForUserAsync(plan.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload updated plan.");
    }
}
