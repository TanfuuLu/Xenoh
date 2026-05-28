using FluentAssertions;
using Xenoh.Application.Features.Users.Preferences;
using Xunit;

namespace Xenoh.Application.Tests.Features.Users;

public sealed class UserPreferenceValidatorTests
{
    [Fact]
    public void NormalizeLanguage_UsesDefault_WhenValueIsEmpty()
    {
        UserPreferenceValidator.NormalizeLanguage("").Should().Be("en");
    }

    [Fact]
    public void NormalizeTheme_AcceptsKnownValues_CaseInsensitively()
    {
        UserPreferenceValidator.NormalizeTheme("DARK").Should().Be("dark");
    }

    [Fact]
    public void NormalizeWeightUnit_RejectsUnknownValues()
    {
        var act = () => UserPreferenceValidator.NormalizeWeightUnit("stone");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid weight unit preference.");
    }
}
