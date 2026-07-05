using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Auth.Commands.ForgotPassword;
using Xenoh.Application.Tests.Common;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class VerifyForgotPasswordCodeHandlerTests : IdentityHandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCodeIsValid_DoesNotThrow()
    {
        await SeedUserAsync("user@test.com", "password123");
        var code = await SendCodeAsync("user@test.com");
        var handler = new VerifyForgotPasswordCodeHandler(CreateUserManager(), CreateContext());

        var act = () => handler.Handle(
            new VerifyForgotPasswordCodeCommand { Email = "user@test.com", Code = code },
            CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenCodeIsInvalid_IncrementsFailedAttempts()
    {
        await SeedUserAsync("user@test.com", "password123");
        await SendCodeAsync("user@test.com");
        var handler = new VerifyForgotPasswordCodeHandler(CreateUserManager(), CreateContext());

        var act = () => handler.Handle(
            new VerifyForgotPasswordCodeCommand { Email = "user@test.com", Code = "000000" },
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid or expired reset code.");

        await using var db = CreateContext();
        var resetCode = await db.PasswordResetCodes.SingleAsync();
        resetCode.FailedAttempts.Should().Be(1);
    }

    private async Task<string> SendCodeAsync(string email)
    {
        var emailService = new FakeEmailService();
        var handler = new SendForgotPasswordCodeHandler(CreateUserManager(), CreateContext(), emailService);

        await handler.Handle(
            new SendForgotPasswordCodeCommand { Email = email },
            CancellationToken.None);

        return emailService.SentCodes.Single().Code;
    }
}
