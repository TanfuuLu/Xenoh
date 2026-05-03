using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Commands.DevActivateSubscription;

/// <summary>
/// Dev/debug helper — directly activates the most recent pending payment order
/// for the authenticated user without requiring a SePay webhook.
/// Only available in Development environment.
/// </summary>
public sealed record DevActivateSubscriptionCommand : IRequest<DevActivateResult>;

public sealed record DevActivateResult(bool Success, string Message);
