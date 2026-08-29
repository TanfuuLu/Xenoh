using FluentAssertions;
using Xenoh.Application.Features.Promotions.Admin;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Promotions;

public sealed class PromotionCodeRulesTests
{
    [Fact]
    public void Validate_PercentDiscountOf100_AcceptsPayload()
    {
        var payload = CreatePayload(100m);

        var validated = PromotionCodeRules.Validate(payload);

        validated.DiscountValue.Should().Be(100m);
    }

    [Fact]
    public void Validate_PercentDiscountAbove100_RejectsPayload()
    {
        var payload = CreatePayload(101m);

        var act = () => PromotionCodeRules.Validate(payload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Percent discount must be between 1 and 100.");
    }

    private static PromotionCodePayload CreatePayload(decimal discountValue) =>
        new(
            "FREE100",
            "Full discount",
            PromotionDiscountType.Percent,
            discountValue,
            PlanTier.ProIndividual,
            10,
            1,
            null,
            null,
            true);
}
