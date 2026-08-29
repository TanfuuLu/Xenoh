using System.Text.RegularExpressions;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;

public sealed class HandleSePayWebhookHandler(
    IPaymentOrderRepository paymentOrderRepo,
    ISubscriptionActivationService subscriptionActivation
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

        await subscriptionActivation.ActivateAsync(order, cancellationToken);

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
