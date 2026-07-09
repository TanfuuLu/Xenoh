using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Auth.Commands.ChangePassword;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class ChangePasswordHandlerTests : IdentityHandlerTestBase
{
    private readonly FakeTokenBlacklist _tokenBlacklist = new();

    private ChangePasswordHandler CreateHandler() =>
        new(CreateUserManager(), CurrentUser(), new RefreshTokenRepository(CreateContext()), _tokenBlacklist);

    [Fact]
    public async Task Handle_WhenCorrectOldPassword_Succeeds()
    {
        await SeedUserWithIdAsync(UserId, "OldPass1");

        await CreateHandler().Handle(new ChangePasswordCommand
        {
            OldPassword = "OldPass1",
            NewPassword = "NewPass2"
        }, CancellationToken.None);

        var userManager = CreateUserManager();
        var user = await userManager.FindByIdAsync(UserId.ToString());
        var checkResult = await userManager.CheckPasswordAsync(user!, "NewPass2");
        checkResult.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOldPasswordWrong_Throws()
    {
        await SeedUserWithIdAsync(UserId, "CorrectPass");

        var act = () => CreateHandler().Handle(new ChangePasswordCommand
        {
            OldPassword = "WrongPass",
            NewPassword = "NewPass2"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Throws()
    {
        var act = () => CreateHandler().Handle(new ChangePasswordCommand
        {
            OldPassword = "OldPass1",
            NewPassword = "NewPass2"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_OnSuccess_RevokesActiveRefreshTokensAndBlacklistsAccessToken()
    {
        await SeedUserWithIdAsync(UserId, "OldPass1");
        await SeedActiveRefreshTokenAsync("active-token");
        const string accessToken = "current-access-token";

        await CreateHandler().Handle(new ChangePasswordCommand
        {
            OldPassword = "OldPass1",
            NewPassword = "NewPass2",
            AccessToken = accessToken
        }, CancellationToken.None);

        var refreshToken = await CreateContext().RefreshTokens
            .SingleAsync(t => t.Token == "active-token");
        refreshToken.IsRevoked.Should().BeTrue();
        _tokenBlacklist.WasRevoked(accessToken).Should().BeTrue();
    }

    private async Task SeedActiveRefreshTokenAsync(string token)
    {
        await using var context = CreateContext();
        context.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            UserId = UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedUserWithIdAsync(Guid id, string password)
    {
        var userManager = CreateUserManager();
        var user = new ApplicationUser
        {
            Id = id,
            Email = "testuser@test.com",
            UserName = "testuser@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, password);
    }
}
