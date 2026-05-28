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
        new(CreateUserManager(), CreateContext());

    [Fact]
    public async Task Handle_WhenValidIndividualRegistration_CreatesUser()
    {
        var dateOfBirth = new DateOnly(2000, 1, 2);
        await SeedRolesAsync();
        var result = await CreateHandler().Handle(new RegisterCommand
        {
            Email = "newuser@test.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Individual,
            Gender = Gender.Male,
            DateOfBirth = dateOfBirth
        }, CancellationToken.None);

        result.Email.Should().Be("newuser@test.com");

        await using var verify = CreateContext();
        var user = await verify.Users.SingleAsync(u => u.Email == "newuser@test.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Gender.Should().Be(Gender.Male);
        user.DateOfBirth.Should().Be(dateOfBirth);
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
            Role = UserRole.Individual,
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(2001, 2, 3)
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
            Role = UserRole.Coach,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(2000, 1, 2)
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
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(2000, 1, 2)
        }, CancellationToken.None);

        await using var verify = CreateContext();
        var user = await verify.Users.SingleAsync(u => u.Id == result.UserId);
        user.AvatarUrl.Should().Be("/assets/avatars/default-male.svg");
    }

    [Fact]
    public async Task Handle_WhenGenderNull_Throws()
    {
        await SeedRolesAsync();
        var act = () => CreateHandler().Handle(new RegisterCommand
        {
            Email = "neutral@test.com",
            Password = "password123",
            FirstName = "Neutral",
            LastName = "User",
            Role = UserRole.Individual,
            Gender = null,
            DateOfBirth = new DateOnly(2000, 1, 2)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Gender is required.");
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
