using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetMySubscription;

public sealed class GetMySubscriptionHandler(
    ISubscriptionRepository subscriptionRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMySubscriptionQuery, SubscriptionResponse>
{
    public async ValueTask<SubscriptionResponse> Handle(
        GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var subscription = await subscriptionRepo.GetByUserIdAsNoTrackingAsync(userId, cancellationToken);

        if (subscription is null)
            return new SubscriptionResponse(Guid.Empty, PlanTier.Free.ToString(), true, null, DateTime.UtcNow);

        return new SubscriptionResponse(
            subscription.Id,
            subscription.Tier.ToString(),
            subscription.IsActive,
            subscription.ExpiresAt,
            subscription.CreatedAt);
    }
}
