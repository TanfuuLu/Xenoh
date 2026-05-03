using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.CreatePlan;

public sealed class CreatePlanHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<CreatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var userId = currentUser.UserId;

        var maxPlans = await subscriptionService.GetMaxPlansAsync(userId, cancellationToken);
        var planCount = await planRepo.CountByOwnerAsync(userId, cancellationToken);
        if (maxPlans != int.MaxValue && planCount >= maxPlans)
            throw new InvalidOperationException(
                $"Your current plan allows a maximum of {maxPlans} plans. Upgrade to Pro for unlimited plans.");

        var plan = new Plan
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanType = PlanType.Self,
            OwnerId = userId
        };

        plan.WeeklyWorkouts = PlanWeekGenerator.Generate(plan);

        await planRepo.AddAsync(plan, cancellationToken);
        await planRepo.SaveChangesAsync(cancellationToken);

        return await planRepo.GetByIdForUserAsync(plan.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload created plan.");
    }
}
