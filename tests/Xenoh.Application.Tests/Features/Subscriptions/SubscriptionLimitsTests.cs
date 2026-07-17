using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionLimitsTests
{
    private const long Mb = 1024L * 1024;

    [Fact]
    public void ProCoachPricing_UsesWebsiteMonthlyPrice()
    {
        SubscriptionLimits.GetListPrice(PlanTier.ProCoach, 1).Should().Be(199_000m);
        SubscriptionLimits.GetPrice(PlanTier.ProCoach, 1).Should().Be(199_000m);
        SubscriptionLimits.GetPrice(PlanTier.ProCoach, 3).Should().Be(597_000m);
        SubscriptionLimits.GetPrice(PlanTier.ProCoach, 6).Should().Be(1_194_000m);
        SubscriptionLimits.GetPrice(PlanTier.ProCoach, 12).Should().Be(2_388_000m);
        SubscriptionLimits.MaxAiRequestsPerMonth(PlanTier.ProCoach).Should().Be(500);
    }

    [Theory]
    [InlineData(PlanTier.Free, 250)]
    [InlineData(PlanTier.ProIndividual, 1024)]
    [InlineData(PlanTier.ProCoach, 5120)]
    [InlineData(PlanTier.Organizer, 5120)]
    public void MaxStorageBytes_UsesPlanStorageLimits(PlanTier tier, long expectedMegabytes)
    {
        SubscriptionLimits.MaxStorageBytes(tier).Should().Be(expectedMegabytes * Mb);
    }

    [Theory]
    [InlineData(PlanTier.Free)]
    [InlineData(PlanTier.ProIndividual)]
    [InlineData(PlanTier.ProCoach)]
    [InlineData(PlanTier.Organizer)]
    public void MaxFileSizeBytes_AllowsDocumentsForEachPlan(PlanTier tier)
    {
        SubscriptionLimits.MaxFileSizeBytes(tier).Should().Be(25L * Mb);
    }

    [Fact]
    public void Organizer_InheritsTopTierProductLimitsWithoutSelfServePrice()
    {
        SubscriptionLimits.MaxPlans(PlanTier.Organizer).Should().Be(int.MaxValue);
        SubscriptionLimits.MaxClients(PlanTier.Organizer).Should().Be(int.MaxValue);
        SubscriptionLimits.CanUseAdvancedAnalytics(PlanTier.Organizer).Should().BeTrue();
        SubscriptionLimits.MaxAiRequestsPerMonth(PlanTier.Organizer).Should().Be(500);
        var getPrice = () => SubscriptionLimits.GetPrice(PlanTier.Organizer, 1);
        getPrice.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MaxCustomExercises_AllowsTenForFreeAndUnlimitedForPaidPlans()
    {
        SubscriptionLimits.MaxCustomExercises(PlanTier.Free).Should().Be(10);
        SubscriptionLimits.MaxCustomExercises(PlanTier.ProIndividual).Should().Be(int.MaxValue);
        SubscriptionLimits.MaxCustomExercises(PlanTier.ProCoach).Should().Be(int.MaxValue);
        SubscriptionLimits.MaxCustomExercises(PlanTier.Organizer).Should().Be(int.MaxValue);
    }
}
