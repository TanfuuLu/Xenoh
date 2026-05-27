using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Auth.Commands.ForgotPassword;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class SendForgotPasswordCodeHandlerTests : IdentityHandlerTestBase
{
    private (SendForgotPasswordCodeHandler Handler, FakeEmailService Email) CreateHandler()
    {
        var email = new FakeEmailService();
        var handler = new SendForgotPasswordCodeHandler(CreateUserManager(), CreateContext(), email);
        return (handler, email);
    }

    [Fact]
    public async Task Handle_WhenValidEmail_SendsCode()
    {
        await SeedUserAsync("user@test.com");

        var (handler, email) = CreateHandler();
        await handler.Handle(new SendForgotPasswordCodeCommand { Email = "user@test.com" }, CancellationToken.None);

        email.SentCodes.Should().HaveCount(1);
        email.SentCodes[0].ToEmail.Should().Be("user@test.com");
        email.SentCodes[0].Code.Should().HaveLength(6);
    }

    [Fact]
    public async Task Handle_WhenUnknownEmail_DoesNotSendAndDoesNotThrow()
    {
        var (handler, email) = CreateHandler();
        var act = () => handler.Handle(new SendForgotPasswordCodeCommand { Email = "nobody@test.com" }, CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
        email.SentCodes.Should().BeEmpty();
    }

    private async Task SeedUserAsync(string userEmail)
    {
        var userManager = CreateUserManager();
        var user = new ApplicationUser
        {
            Id = UserId,
            Email = userEmail,
            UserName = userEmail,
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "password123");
    }
}
