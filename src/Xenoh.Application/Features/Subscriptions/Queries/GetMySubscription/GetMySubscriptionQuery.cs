using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetMySubscription;

public sealed record GetMySubscriptionQuery : IRequest<SubscriptionResponse>;

public sealed record SubscriptionResponse(
    Guid Id,
    string Tier,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    AiQuotaResponse AiQuota
);

public sealed record AiQuotaResponse(
    int MonthlyLimit,
    int UsedRequests,
    int RemainingRequests,
    DateOnly PeriodStart
);
