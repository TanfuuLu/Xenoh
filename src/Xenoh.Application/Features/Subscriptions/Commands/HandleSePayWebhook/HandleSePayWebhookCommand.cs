using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;

public sealed record HandleSePayWebhookCommand(
    long SePayId,
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string? Code,
    string? Content,
    string TransferType,
    decimal TransferAmount,
    string? ReferenceCode,
    string? Description
) : IRequest<WebhookResult>;

public sealed record WebhookResult(bool Success, string Message);
