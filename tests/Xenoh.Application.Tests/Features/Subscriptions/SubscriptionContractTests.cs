using FluentAssertions;
using Xenoh.Application.Features.Subscriptions;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionContractTests
{
    [Theory]
    [InlineData(true, "2026-08-02")]
    [InlineData(false, "2026-08-02")]
    [InlineData(true, "2026-06-16")]
    [InlineData(false, null)]
    public void EnsureCurrentTermsAccepted_RejectsMissingOrStaleAcceptance(
        bool accepted,
        string? version)
    {
        var act = () => SubscriptionContract.EnsureCurrentTermsAccepted(accepted, version);

        if (accepted && version == SubscriptionContract.CurrentTermsVersion)
            act.Should().NotThrow();
        else
            act.Should().Throw<InvalidOperationException>();
    }
}
