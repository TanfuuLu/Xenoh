using FluentAssertions;
using Xenoh.Application.Features.Users.Commands.UpdateMyPreferences;
using Xenoh.Application.Features.Users.Queries.GetMyPreferences;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xunit;

namespace Xenoh.Application.Tests.Features.Users;

public sealed class UserPreferencesHandlerTests : IdentityHandlerTestBase
{
    [Fact]
    public void ApplicationUser_DefaultsTrackRpeToEnabled()
    {
        new ApplicationUser().TrackRpe.Should().BeTrue();
    }

    [Fact]
    public async Task GetPreferences_ReturnsStoredTrackRpeValue()
    {
        await SeedUserAsync("preferences@test.com", "secret1");
        await SetTrackRpeAsync(false);

        var handler = new GetMyPreferencesHandler(CreateUserManager(), CurrentUser());
        var response = await handler.Handle(new GetMyPreferencesQuery(), CancellationToken.None);

        response.TrackRpe.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdatePreferences_PersistsTrackRpeValue(bool trackRpe)
    {
        await SeedUserAsync("update-preferences@test.com", "secret1");
        var handler = new UpdateMyPreferencesHandler(CreateUserManager(), CurrentUser());

        var response = await handler.Handle(
            new UpdateMyPreferencesCommand("en", "light", "kg", trackRpe),
            CancellationToken.None);

        response.TrackRpe.Should().Be(trackRpe);
        var storedUser = await CreateUserManager().FindByIdAsync(UserId.ToString());
        storedUser!.TrackRpe.Should().Be(trackRpe);
    }

    [Fact]
    public async Task UpdatePreferences_WhenTrackRpeIsOmitted_PreservesStoredValue()
    {
        await SeedUserAsync("legacy-preferences@test.com", "secret1");
        await SetTrackRpeAsync(false);
        var handler = new UpdateMyPreferencesHandler(CreateUserManager(), CurrentUser());

        var response = await handler.Handle(
            new UpdateMyPreferencesCommand("en", "light", "kg", null),
            CancellationToken.None);

        response.TrackRpe.Should().BeFalse();
        var storedUser = await CreateUserManager().FindByIdAsync(UserId.ToString());
        storedUser!.TrackRpe.Should().BeFalse();
    }

    private async Task SetTrackRpeAsync(bool trackRpe)
    {
        var userManager = CreateUserManager();
        var user = await userManager.FindByIdAsync(UserId.ToString());
        user!.TrackRpe = trackRpe;
        var result = await userManager.UpdateAsync(user);
        result.Succeeded.Should().BeTrue();
    }
}
