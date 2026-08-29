using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions.Commands.DevActivateSubscription;

public sealed class DevActivateSubscriptionHandler(
    IPaymentOrderRepository paymentOrderRepo,
    ISubscriptionRepository subscriptionRepo,
    ICurrentUserService currentUser,
    ISubscriptionActivationService subscriptionActivation
) : IRequestHandler<DevActivateSubscriptionCommand, DevActivateResult>
{
    public async ValueTask<DevActivateResult> Handle(
        DevActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Find the most recent pending order for this user
        var order = await paymentOrderRepo.FindLatestPendingByUserAsync(userId, cancellationToken);
        if (order is null)
            return new DevActivateResult(false, "No pending payment order found for your account.");

        if (order.ExpiresAt < DateTime.UtcNow)
            return new DevActivateResult(false, $"Order {order.TransferCode} has expired. Create a new order.");

        // Mark order as completed
        order.Status = PaymentStatus.Completed;
        order.SePayTransactionId = $"DEV_{Guid.NewGuid():N}";
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await subscriptionActivation.ActivateAsync(order, cancellationToken);

        var subscription = await subscriptionRepo.FindByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription row not found for user {userId}.");

        return new DevActivateResult(
            true,
            $"Activated {order.RequestedTier} for {order.DurationMonths} month(s). Expires {subscription.ExpiresAt:dd/MM/yyyy}.");
    }
}
