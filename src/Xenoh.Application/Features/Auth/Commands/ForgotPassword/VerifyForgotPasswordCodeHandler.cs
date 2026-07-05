using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed class VerifyForgotPasswordCodeHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db
) : IRequestHandler<VerifyForgotPasswordCodeCommand>
{
    public async ValueTask<Unit> Handle(VerifyForgotPasswordCodeCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new InvalidOperationException("Invalid or expired reset code.");

        await PasswordResetCodeVerifier.VerifyAsync(
            user,
            request.Code,
            db,
            DateTime.UtcNow,
            cancellationToken);

        return Unit.Value;
    }
}
