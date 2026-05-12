using FluentAssertions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;
using Xenoh.Application.Tests.Common;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionServiceTests : HandlerTestBase
{
    [Fact]
    public async Task GetActiveTierAsync_WhenSubscriptionMissing_ReturnsFree()
    {
        await using var db = CreateContext();
        var service = new SubscriptionService(new SubscriptionRepository(db));

        var tier = await service.GetActiveTierAsync(UserId);

        tier.Should().Be(PlanTier.Free);
    }

    [Fact]
    public async Task GetActiveTierAsync_WhenPaidSubscriptionExpired_ReturnsFree()
    {
        await using var db = CreateContext();
        db.UserSubscriptions.Add(new UserSubscription
        {
            UserId = UserId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(new SubscriptionRepository(db));

        var tier = await service.GetActiveTierAsync(UserId);

        tier.Should().Be(PlanTier.Free);
    }

    [Theory]
    [InlineData(PlanTier.ProIndividual)]
    [InlineData(PlanTier.ProCoach)]
    public async Task GetActiveTierAsync_WhenPaidSubscriptionActive_ReturnsPaidTier(PlanTier expectedTier)
    {
        await using var db = CreateContext();
        db.UserSubscriptions.Add(new UserSubscription
        {
            UserId = UserId,
            Tier = expectedTier,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(new SubscriptionRepository(db));

        var tier = await service.GetActiveTierAsync(UserId);

        tier.Should().Be(expectedTier);
    }
}
