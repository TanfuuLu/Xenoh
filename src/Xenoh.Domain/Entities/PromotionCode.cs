using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class PromotionCode : BaseEntity
{
    /// <summary>Uppercase alphanumeric code users type at checkout (unique).</summary>
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public PromotionDiscountType DiscountType { get; set; }

    /// <summary>Percent (1–99) or fixed VND amount depending on DiscountType.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Restrict the code to one paid tier; null = valid for any paid tier.</summary>
    public PlanTier? AppliesToTier { get; set; }

    /// <summary>Total redemptions across all users; null = unlimited.</summary>
    public int? MaxRedemptions { get; set; }

    public int MaxRedemptionsPerUser { get; set; } = 1;

    /// <summary>Code is unusable before this moment; null = usable immediately.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Code is unusable after this moment; null = never expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PaymentOrder> PaymentOrders { get; set; } = [];
}
