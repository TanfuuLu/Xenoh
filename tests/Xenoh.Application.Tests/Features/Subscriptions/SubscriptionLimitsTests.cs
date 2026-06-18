using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionLimitsTests
{
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
}
