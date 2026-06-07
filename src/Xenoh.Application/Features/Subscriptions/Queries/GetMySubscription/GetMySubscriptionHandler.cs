using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetMySubscription;

public sealed class GetMySubscriptionHandler(
    ISubscriptionRepository subscriptionRepo,
    ICurrentUserService currentUser,
    IAiQuotaService aiQuotaService
) : IRequestHandler<GetMySubscriptionQuery, SubscriptionResponse>
{
    public async ValueTask<SubscriptionResponse> Handle(
        GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var subscription = await subscriptionRepo.GetByUserIdAsNoTrackingAsync(userId, cancellationToken);
        var quota = await aiQuotaService.GetCurrentAsync(cancellationToken);
        var quotaResponse = new AiQuotaResponse(
            quota.MonthlyLimit,
            quota.UsedRequests,
            quota.RemainingRequests,
            quota.PeriodStart);

        if (subscription is null)
            return new SubscriptionResponse(
                Guid.Empty,
                PlanTier.Free.ToString(),
                true,
                null,
                DateTime.UtcNow,
                quotaResponse);

        return new SubscriptionResponse(
            subscription.Id,
            subscription.Tier.ToString(),
            subscription.IsActive,
            subscription.ExpiresAt,
            subscription.CreatedAt,
            quotaResponse);
    }
}
