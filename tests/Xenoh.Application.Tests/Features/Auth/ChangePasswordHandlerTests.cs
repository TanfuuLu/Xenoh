using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Auth.Commands.ChangePassword;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class ChangePasswordHandlerTests : IdentityHandlerTestBase
{
    private ChangePasswordHandler CreateHandler() =>
        new(CreateUserManager(), CurrentUser());

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
