using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Auth.Commands.Register;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class RegisterHandlerTests : IdentityHandlerTestBase
{
    private RegisterHandler CreateHandler() =>
        new(CreateUserManager(), new FakeTokenService(), CreateContext());

    [Fact]
    public async Task Handle_WhenValidIndividualRegistration_ReturnsTokens()
    {
        await SeedRolesAsync();
        var result = await CreateHandler().Handle(new RegisterCommand
        {
            Email = "newuser@test.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Individual
        }, CancellationToken.None);

        result.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.Email.Should().Be("newuser@test.com");
        result.FullName.Should().Be("John Doe");

        await using var verify = CreateContext();
        var userExists = await verify.Users.AnyAsync(u => u.Email == "newuser@test.com");
        userExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_Throws()
    {
        await SeedUserAsync("existing@test.com", "password123");

        var act = () => CreateHandler().Handle(new RegisterCommand
        {
            Email = "existing@test.com",
            Password = "password456",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Individual
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is already registered.");
    }

    [Fact]
    public async Task Handle_WhenCoachRoleRequested_Throws()
    {
        var act = () => CreateHandler().Handle(new RegisterCommand
        {
            Email = "coach@test.com",
            Password = "password123",
            FirstName = "Coach",
            LastName = "User",
            Role = UserRole.Coach
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task Handle_WhenGenderMale_SetsDefaultMaleAvatar()
    {
        await SeedRolesAsync();
        var result = await CreateHandler().Handle(new RegisterCommand
        {
            Email = "male@test.com",
            Password = "password123",
            FirstName = "Male",
            LastName = "User",
            Role = UserRole.Individual,
            Gender = Gender.Male
        }, CancellationToken.None);

        result.AvatarUrl.Should().Be("/assets/avatars/default-male.svg");
    }

    [Fact]
    public async Task Handle_WhenGenderNull_SetsNeutralAvatar()
    {
        await SeedRolesAsync();
        var result = await CreateHandler().Handle(new RegisterCommand
        {
            Email = "neutral@test.com",
            Password = "password123",
            FirstName = "Neutral",
            LastName = "User",
            Role = UserRole.Individual,
            Gender = null
        }, CancellationToken.None);

        result.AvatarUrl.Should().Be("/assets/avatars/default-neutral.svg");
    }

    private async Task SeedUserAsync(string email, string password)
    {
        var userManager = CreateUserManager();
        var user = new Domain.Entities.ApplicationUser
        {
            Email = email,
            UserName = email,
            FirstName = "Existing",
            LastName = "User"
        };
        await userManager.CreateAsync(user, password);
    }
}
