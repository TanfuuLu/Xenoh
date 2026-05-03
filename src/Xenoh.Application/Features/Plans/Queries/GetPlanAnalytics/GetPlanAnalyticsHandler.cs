using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

public sealed class GetPlanAnalyticsHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<GetPlanAnalyticsQuery, PlanAnalyticsResponse>
{
    public async ValueTask<PlanAnalyticsResponse> Handle(
        GetPlanAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (!await subscriptionService.CanUseAdvancedAnalyticsAsync(userId, cancellationToken))
            throw new InvalidOperationException("Advanced analytics requires an active Pro subscription.");

        return await planRepo.GetAnalyticsAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");
    }
}
