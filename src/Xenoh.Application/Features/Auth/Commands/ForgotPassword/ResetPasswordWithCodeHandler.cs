using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ResetPasswordWithCodeHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db
) : IRequestHandler<ResetPasswordWithCodeCommand>
{
    public async ValueTask<Unit> Handle(ResetPasswordWithCodeCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new InvalidOperationException("Invalid or expired reset code.");

        var now = DateTime.UtcNow;
        var resetCode = await PasswordResetCodeVerifier.VerifyAsync(
            user,
            request.Code,
            db,
            now,
            cancellationToken);

        var result = await userManager.ResetPasswordAsync(user, resetCode.ResetToken, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        resetCode.UsedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
