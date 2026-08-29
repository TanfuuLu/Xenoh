namespace Xenoh.Domain.Enums;

public enum PromotionDiscountType
{
    /// <summary>DiscountValue is a percentage of the order amount (1–100).</summary>
    Percent = 0,

    /// <summary>DiscountValue is a fixed VND amount subtracted from the order.</summary>
    FixedAmount = 1
}
