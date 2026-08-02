using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionCatalogTests
{
    [Fact]
    public void PublicPlans_DefinesEveryPurchasableTierAndDurationExactlyOnce()
    {
        var plans = SubscriptionCatalog.PublicPlans;

        plans.Should().HaveCount(8);
        plans.Select(x => (x.Tier, x.DurationMonths)).Should().OnlyHaveUniqueItems();
        plans.Select(x => x.Tier).Distinct().Should().BeEquivalentTo(
            [PlanTier.ProIndividual, PlanTier.ProCoach]);
        plans.Select(x => x.DurationMonths).Distinct().Should().BeEquivalentTo([1, 3, 6, 12]);
    }

    [Fact]
    public void PublicPlans_AreFixedPrepaidVndOffersWithoutAutomaticRenewal()
    {
        SubscriptionCatalog.PublicPlans.Should().OnlyContain(x =>
            x.Currency == "VND" &&
            x.Price > 0 &&
            x.IsPrepaid &&
            !x.AutomaticallyRenews);
    }

    [Theory]
    [InlineData(PlanTier.ProIndividual, 1, 100_000)]
    [InlineData(PlanTier.ProIndividual, 12, 1_200_000)]
    [InlineData(PlanTier.ProCoach, 1, 199_000)]
    [InlineData(PlanTier.ProCoach, 12, 2_388_000)]
    public void GetRequired_ReturnsServerAuthoritativePrice(
        PlanTier tier,
        int durationMonths,
        decimal expectedPrice)
    {
        var plan = SubscriptionCatalog.GetRequired(tier, durationMonths);

        plan.Price.Should().Be(expectedPrice);
        SubscriptionLimits.GetPrice(tier, durationMonths).Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData(PlanTier.Free, 1)]
    [InlineData(PlanTier.Organizer, 1)]
    [InlineData(PlanTier.ProCoach, 2)]
    public void GetRequired_RejectsNonPurchasableCombination(PlanTier tier, int durationMonths)
    {
        var act = () => SubscriptionCatalog.GetRequired(tier, durationMonths);

        act.Should().Throw<InvalidOperationException>();
    }
}
