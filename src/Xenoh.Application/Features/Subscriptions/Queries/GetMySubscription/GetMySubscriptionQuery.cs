using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetMySubscription;

public sealed record GetMySubscriptionQuery : IRequest<SubscriptionResponse>;

public sealed record SubscriptionResponse(
    Guid Id,
    string Tier,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);
