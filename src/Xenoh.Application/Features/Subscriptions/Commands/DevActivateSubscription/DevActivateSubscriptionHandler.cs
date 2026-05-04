using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;

namespace Xenoh.Application.Features.Subscriptions.Commands.DevActivateSubscription;

public sealed class DevActivateSubscriptionHandler(
    IPaymentOrderRepository paymentOrderRepo,
    ISubscriptionRepository subscriptionRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
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

        // Activate/extend subscription
        var subscription = await subscriptionRepo.FindByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription row not found for user {userId}.");

        var now = DateTime.UtcNow;
        var currentExpiry = (subscription.ExpiresAt.HasValue && subscription.ExpiresAt > now)
            ? subscription.ExpiresAt.Value
            : now;

        subscription.Tier = order.RequestedTier;
        subscription.ExpiresAt = currentExpiry.AddMonths(order.DurationMonths);
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepo.SaveChangesAsync(cancellationToken);

        // Sync Coach role: grant when ProCoach activates, revoke on any other tier
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            if (order.RequestedTier == PlanTier.ProCoach)
            {
                if (!await userManager.IsInRoleAsync(user, UserRole.Coach))
                    await userManager.AddToRoleAsync(user, UserRole.Coach);
            }
            else
            {
                if (await userManager.IsInRoleAsync(user, UserRole.Coach))
                    await userManager.RemoveFromRoleAsync(user, UserRole.Coach);
            }
        }

        return new DevActivateResult(
            true,
            $"Activated {order.RequestedTier} for {order.DurationMonths} month(s). Expires {subscription.ExpiresAt:dd/MM/yyyy}.");
    }
}
