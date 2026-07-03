using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Commands.CheckSubscriptionExpiry;

public sealed record CheckSubscriptionExpiryResult(int RemindedCount, int ExpiredCount);

public sealed record CheckSubscriptionExpiryCommand : IRequest<CheckSubscriptionExpiryResult>;
