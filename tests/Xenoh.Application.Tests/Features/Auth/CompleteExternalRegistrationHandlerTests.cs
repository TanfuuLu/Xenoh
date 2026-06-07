using FluentAssertions;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class CompleteExternalRegistrationHandlerTests : IdentityHandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCoachRoleRequested_Throws()
    {
        await SeedRolesAsync();
        await SeedUserAsync(UserId, "oauth@test.com", "password123");

        await using var db = CreateContext();
        var handler = new CompleteExternalRegistrationHandler(
            CreateUserManager(),
            CurrentUser(),
            new FakeTokenService(),
            new RefreshTokenRepository(db));

        var act = () => handler.Handle(new CompleteExternalRegistrationCommand
        {
            Role = UserRole.Coach
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Individual accounts only*");
    }

    [Fact]
    public async Task Handle_WhenIndividualRoleRequested_AddsIndividualRole()
    {
        await SeedRolesAsync();
        await SeedUserAsync(UserId, "oauth-individual@test.com", "password123");

        await using var db = CreateContext();
        var handler = new CompleteExternalRegistrationHandler(
            CreateUserManager(),
            CurrentUser(),
            new FakeTokenService(),
            new RefreshTokenRepository(db));

        var result = await handler.Handle(new CompleteExternalRegistrationCommand
        {
            Role = UserRole.Individual
        }, CancellationToken.None);

        result.Roles.Should().ContainSingle(UserRole.Individual);
    }
}
