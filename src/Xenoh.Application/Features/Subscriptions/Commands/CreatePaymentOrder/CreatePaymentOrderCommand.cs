using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions.Commands.CreatePaymentOrder;

public sealed record CreatePaymentOrderCommand : IRequest<PaymentOrderResponse>
{
    [Required]
    public required PlanTier RequestedTier { get; init; }

    [Required]
    [Range(1, 12)]
    public required int DurationMonths { get; init; }

    [MaxLength(40)]
    public string? PromotionCode { get; init; }

    public bool AcceptedTerms { get; init; }

    [MaxLength(40)]
    public string? TermsVersion { get; init; }
}

public sealed record PaymentOrderResponse(
    Guid OrderId,
    string TransferCode,
    decimal Amount,
    decimal OriginalAmount,
    decimal DiscountAmount,
    string? PromotionCode,
    int DurationMonths,
    string RequestedTier,
    DateTime ExpiresAt,
    string BankAccountNumber,
    string BankAccountName,
    string BankName,
    string TransferDescription,
    bool PaymentRequired
);
