using FluentAssertions;
using Xenoh.Domain.Rules;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class ProgressLibraryRulesTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void HasValidPhotoCount_AllowsOneToThreePhotos(int count, bool expected)
    {
        ProgressLibraryRules.HasValidPhotoCount(count).Should().Be(expected);
    }

    [Fact]
    public void HasValidCheckInDate_RejectsFutureDates()
    {
        ProgressLibraryRules.HasValidCheckInDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .Should().BeFalse();
    }
}
