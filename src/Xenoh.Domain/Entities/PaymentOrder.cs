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

    /// <summary>Final amount the user must transfer (after any promotion discount).</summary>
    public decimal Amount { get; set; }

    /// <summary>VND knocked off the list price by a promotion code; 0 when none applied.</summary>
    public decimal DiscountAmount { get; set; }

    public Guid? PromotionCodeId { get; set; }
    public PromotionCode? PromotionCode { get; set; }

    public int DurationMonths { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime ExpiresAt { get; set; }

    // Filled when SePay confirms payment
    public string? SePayTransactionId { get; set; }
    public string? SePayReferenceCode { get; set; }
    public DateTime? PaidAt { get; set; }
}
