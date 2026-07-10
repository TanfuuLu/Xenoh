using FluentAssertions;
using Xunit;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Infrastructure;

public sealed class CachingContractTests
{
    [Fact]
    public void UserTags_AreScopedToOneUser()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        CacheTags.User(first).Should().NotBe(CacheTags.User(second));
        CacheTags.User(first).Should().Be($"user:{first:N}");
    }

    [Fact]
    public async Task FakeCache_ReusesAResponseForTheSameTagAndKey()
    {
        var cache = new Xenoh.Application.Tests.Common.FakeApplicationCache();
        var first = await cache.GetOrCreateAsync(CacheTags.Leaderboards, "dots", TimeSpan.FromSeconds(30),
            _ => Task.FromResult(Guid.NewGuid()));
        var second = await cache.GetOrCreateAsync(CacheTags.Leaderboards, "dots", TimeSpan.FromSeconds(30),
            _ => Task.FromResult(Guid.NewGuid()));

        second.Should().Be(first);
        cache.FactoryCalls.Should().Be(1);
    }
}
