using FluentAssertions;
using Xenoh.Application.Features.Promotions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Promotions;

public sealed class PromotionEvaluationTests
{
    [Theory]
    [InlineData(" summer25 ", "SUMMER25")]
    [InlineData("Xenoh10", "XENOH10")]
    public void NormalizeCode_TrimsAndUppercases(string input, string expected)
    {
        PromotionEvaluation.NormalizeCode(input).Should().Be(expected);
    }

    [Fact]
    public void ComputeDiscount_Percent_RoundsToWholeVnd()
    {
        var promo = new PromotionCode { DiscountType = PromotionDiscountType.Percent, DiscountValue = 20 };

        PromotionEvaluation.ComputeDiscount(promo, 149_000m).Should().Be(29_800m);
    }

    [Fact]
    public void ComputeDiscount_FixedAmount_SubtractsFlatValue()
    {
        var promo = new PromotionCode { DiscountType = PromotionDiscountType.FixedAmount, DiscountValue = 50_000 };

        PromotionEvaluation.ComputeDiscount(promo, 149_000m).Should().Be(50_000m);
    }

    [Fact]
    public void ComputeDiscount_NeverLeavesLessThanMinimumPayable()
    {
        var promo = new PromotionCode { DiscountType = PromotionDiscountType.FixedAmount, DiscountValue = 400_000 };

        var discount = PromotionEvaluation.ComputeDiscount(promo, 149_000m);

        discount.Should().Be(149_000m - PromotionEvaluation.MinimumPayableVnd);
        (149_000m - discount).Should().Be(PromotionEvaluation.MinimumPayableVnd);
    }

    [Fact]
    public void ComputeDiscount_MaxPercent_StillLeavesPayableAmount()
    {
        var promo = new PromotionCode { DiscountType = PromotionDiscountType.Percent, DiscountValue = 99 };

        var discount = PromotionEvaluation.ComputeDiscount(promo, 149_000m);

        (149_000m - discount).Should().BeGreaterThanOrEqualTo(PromotionEvaluation.MinimumPayableVnd);
    }

    [Fact]
    public void ComputeDiscount_FullPercent_CoversEntireAmount()
    {
        var promo = new PromotionCode { DiscountType = PromotionDiscountType.Percent, DiscountValue = 100 };

        var discount = PromotionEvaluation.ComputeDiscount(promo, 149_000m);

        discount.Should().Be(149_000m);
    }
}
