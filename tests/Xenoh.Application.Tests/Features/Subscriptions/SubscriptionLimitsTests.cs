using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionLimitsTests
{
    [Fact]
    public void ProCoachPricing_UsesLaunchPromoAgainst299KListPrice()
    {
        SubscriptionLimits.GetListPrice(PlanTier.ProCoach, 1).Should().Be(299_000m);
        SubscriptionLimits.GetPrice(PlanTier.ProCoach, 1).Should().Be(199_000m);
        SubscriptionLimits.MaxAiRequestsPerMonth(PlanTier.ProCoach).Should().Be(500);
    }
}
