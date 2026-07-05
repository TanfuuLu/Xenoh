using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Promotions.Queries.ValidatePromotionCode;

public sealed record ValidatePromotionCodeQuery : IRequest<ValidatePromotionCodeResponse>
{
    [Required]
    [MaxLength(40)]
    public required string Code { get; init; }

    /// <summary>When provided together with DurationMonths, the response includes exact amounts.</summary>
    public PlanTier? RequestedTier { get; init; }

    [Range(1, 12)]
    public int? DurationMonths { get; init; }
}

public sealed record ValidatePromotionCodeResponse(
    bool Valid,
    string? Message,
    string? Code,
    PromotionDiscountType? DiscountType,
    decimal DiscountValue,
    PlanTier? AppliesToTier,
    decimal? OriginalAmount,
    decimal? DiscountAmount,
    decimal? FinalAmount
)
{
    public static ValidatePromotionCodeResponse Invalid(string message) =>
        new(false, message, null, null, 0, null, null, null, null);
}
