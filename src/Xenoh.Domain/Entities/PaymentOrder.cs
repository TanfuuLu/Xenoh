using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class PaymentOrder : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid SubscriptionId { get; set; }
    public UserSubscription Subscription { get; set; } = null!;

    public PlanTier RequestedTier { get; set; }

    /// Format: XENOH_{8hexUserId}_{8hexOrderId} — matched by SePay via bank transfer description
    public string TransferCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int DurationMonths { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime ExpiresAt { get; set; }

    // Filled when SePay confirms payment
    public string? SePayTransactionId { get; set; }
    public string? SePayReferenceCode { get; set; }
    public DateTime? PaidAt { get; set; }
}
