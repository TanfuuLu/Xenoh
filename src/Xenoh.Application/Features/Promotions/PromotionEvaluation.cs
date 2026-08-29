using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Promotions;

/// <summary>
/// Shared promotion-code rules used by both the user-facing validate endpoint and
/// payment-order creation, so a code can never pass preview but fail checkout (or vice versa).
/// </summary>
public static class PromotionEvaluation
{
    /// <summary>Bank transfers can't be zero, so a discount always leaves this much to pay.</summary>
    public const decimal MinimumPayableVnd = 1_000m;

    public static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    /// <summary>
    /// Returns an error message when the code can't be used by this user right now, or null when usable.
    /// Redemptions are counted from completed payment orders — nothing is "consumed" until money arrives,
    /// so an abandoned pending order never burns a redemption.
    /// </summary>
    public static async Task<string?> CheckEligibilityAsync(
        IApplicationDbContext db, PromotionCode promo, Guid userId, PlanTier? requestedTier, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (!promo.IsActive)
            return "This promotion code is no longer active.";
        if (promo.StartsAt.HasValue && promo.StartsAt > now)
            return "This promotion code is not active yet.";
        if (promo.ExpiresAt.HasValue && promo.ExpiresAt < now)
            return "This promotion code has expired.";
        if (requestedTier.HasValue && promo.AppliesToTier.HasValue && promo.AppliesToTier != requestedTier)
            return $"This promotion code only applies to the {promo.AppliesToTier} plan.";

        var completedUses = await db.PaymentOrders.AsNoTracking()
            .Where(o => o.PromotionCodeId == promo.Id && o.Status == PaymentStatus.Completed)
            .Select(o => o.UserId)
            .ToListAsync(ct);

        if (promo.MaxRedemptions.HasValue && completedUses.Count >= promo.MaxRedemptions.Value)
            return "This promotion code has reached its redemption limit.";
        if (completedUses.Count(u => u == userId) >= promo.MaxRedemptionsPerUser)
            return "You have already used this promotion code.";

        return null;
    }

    public static decimal ComputeDiscount(PromotionCode promo, decimal amount)
    {
        if (promo.DiscountType == PromotionDiscountType.Percent && promo.DiscountValue == 100m)
            return Math.Max(0m, amount);

        var discount = promo.DiscountType == PromotionDiscountType.Percent
            ? Math.Round(amount * promo.DiscountValue / 100m, 0, MidpointRounding.AwayFromZero)
            : promo.DiscountValue;

        return Math.Clamp(discount, 0m, Math.Max(0m, amount - MinimumPayableVnd));
    }
}
