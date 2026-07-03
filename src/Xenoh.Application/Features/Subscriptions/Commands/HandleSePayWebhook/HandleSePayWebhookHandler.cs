using System.Text.RegularExpressions;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;

namespace Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;

public sealed class HandleSePayWebhookHandler(
    IPaymentOrderRepository paymentOrderRepo,
    ISubscriptionRepository subscriptionRepo,
    UserManager<ApplicationUser> userManager,
    INotificationService notificationService
) : IRequestHandler<HandleSePayWebhookCommand, WebhookResult>
{
    private static readonly Regex TransferCodePattern =
        new(@"XENOH[0-9A-F]{16}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async ValueTask<WebhookResult> Handle(
        HandleSePayWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.TransferType != "in")
            return new WebhookResult(true, "Ignored: not an inbound transfer.");

        var transferCode = ExtractTransferCode(request.Code, request.Content, request.Description);
        if (string.IsNullOrWhiteSpace(transferCode))
            return new WebhookResult(true, "Ignored: no recognizable transfer code.");

        // Idempotency: check if this SePay transaction was already processed
        var sePayTxId = request.SePayId.ToString();
        var duplicate = await paymentOrderRepo.FindBySePayTransactionIdAsync(sePayTxId, cancellationToken);
        if (duplicate is not null)
            return new WebhookResult(true, "Already processed.");

        var order = await paymentOrderRepo.FindByTransferCodeAsync(transferCode, cancellationToken);
        if (order is null)
            return new WebhookResult(true, "Transfer code not found — unrelated transaction.");

        if (order.Status != PaymentStatus.Pending)
            return new WebhookResult(true, $"Order already in status {order.Status}.");

        if (order.ExpiresAt < DateTime.UtcNow)
        {
            order.Status = PaymentStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;
            await paymentOrderRepo.SaveChangesAsync(cancellationToken);
            return new WebhookResult(true, "Order expired.");
        }

        if (request.TransferAmount < order.Amount)
        {
            order.Status = PaymentStatus.Failed;
            order.UpdatedAt = DateTime.UtcNow;
            await paymentOrderRepo.SaveChangesAsync(cancellationToken);
            return new WebhookResult(true, "Insufficient transfer amount.");
        }

        order.Status = PaymentStatus.Completed;
        order.SePayTransactionId = sePayTxId;
        order.SePayReferenceCode = request.ReferenceCode;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        var subscription = await subscriptionRepo.FindByUserIdAsync(order.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription not found for user {order.UserId}.");

        var now = DateTime.UtcNow;

        // Same-tier renewal stacks onto remaining time; switching tier (upgrade/downgrade)
        // starts a fresh term from payment time — remaining days do not carry over.
        var isSameTierRenewal = subscription.Tier == order.RequestedTier
            && subscription.ExpiresAt.HasValue
            && subscription.ExpiresAt > now;
        var baseDate = isSameTierRenewal ? subscription.ExpiresAt!.Value : now;

        subscription.Tier = order.RequestedTier;
        subscription.ExpiresAt = baseDate.AddMonths(order.DurationMonths);
        subscription.ExpiryReminderSentAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await subscriptionRepo.SaveChangesAsync(cancellationToken);

        // Sync Coach role: grant when ProCoach activates, revoke on any other tier
        var user = await userManager.FindByIdAsync(order.UserId.ToString());
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

        await notificationService.NotifyAsync(
            order.UserId,
            "SubscriptionActivated",
            $"Đăng ký gói {order.RequestedTier} đã được kích hoạt thành công. Hết hạn vào {subscription.ExpiresAt:dd/MM/yyyy}.",
            subscription.Id,
            "Subscription",
            cancellationToken);

        return new WebhookResult(true, "Subscription activated.");
    }

    private string? ExtractTransferCode(params string?[] sources)
    {
        foreach (var src in sources)
        {
            if (string.IsNullOrWhiteSpace(src)) continue;
            var match = TransferCodePattern.Match(src);
            if (match.Success) return match.Value.ToUpperInvariant();
        }
        return null;
    }
}
