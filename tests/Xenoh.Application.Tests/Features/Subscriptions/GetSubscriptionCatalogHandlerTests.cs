using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Application.Features.Subscriptions.Queries.GetSubscriptionCatalog;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class GetSubscriptionCatalogHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTheCanonicalFixedPrepaidCatalog()
    {
        var result = await new GetSubscriptionCatalogHandler().Handle(
            new GetSubscriptionCatalogQuery(),
            CancellationToken.None);

        result.TermsVersion.Should().Be(SubscriptionContract.CurrentTermsVersion);
        result.Offers.Should().HaveCount(8);
        result.Offers.Should().OnlyContain(x =>
            x.Currency == "VND" && x.IsPrepaid && !x.AutomaticallyRenews);
        result.Offers.Single(x => x.Tier == "ProCoach" && x.DurationMonths == 1)
            .HasUnlimitedClients.Should().BeTrue();
    }
}
