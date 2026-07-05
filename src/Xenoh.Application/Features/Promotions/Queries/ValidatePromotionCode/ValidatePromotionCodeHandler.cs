using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Promotions.Queries.ValidatePromotionCode;

public sealed class ValidatePromotionCodeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<ValidatePromotionCodeQuery, ValidatePromotionCodeResponse>
{
    public async ValueTask<ValidatePromotionCodeResponse> Handle(
        ValidatePromotionCodeQuery request, CancellationToken cancellationToken)
    {
        var code = PromotionEvaluation.NormalizeCode(request.Code);
        if (code.Length == 0)
            return ValidatePromotionCodeResponse.Invalid("Please enter a promotion code.");

        var promo = await db.PromotionCodes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
        if (promo is null)
            return ValidatePromotionCodeResponse.Invalid("Invalid promotion code.");

        var error = await PromotionEvaluation.CheckEligibilityAsync(
            db, promo, currentUser.UserId, request.RequestedTier, cancellationToken);
        if (error is not null)
            return ValidatePromotionCodeResponse.Invalid(error);

        decimal? originalAmount = null, discountAmount = null, finalAmount = null;
        if (request.RequestedTier is { } tier && tier != PlanTier.Free && request.DurationMonths is { } months)
        {
            try
            {
                var amount = SubscriptionLimits.GetPrice(tier, months);
                originalAmount = amount;
                discountAmount = PromotionEvaluation.ComputeDiscount(promo, amount);
                finalAmount = amount - discountAmount;
            }
            catch
            {
                return ValidatePromotionCodeResponse.Invalid("Invalid tier/duration combination.");
            }
        }

        return new ValidatePromotionCodeResponse(
            true,
            null,
            promo.Code,
            promo.DiscountType,
            promo.DiscountValue,
            promo.AppliesToTier,
            originalAmount,
            discountAmount,
            finalAmount);
    }
}
