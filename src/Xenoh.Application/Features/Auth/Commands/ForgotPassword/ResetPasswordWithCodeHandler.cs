using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ResetPasswordWithCodeHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IRefreshTokenRepository refreshTokenRepo
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

        // A password reset is a recovery action — evict any sessions an attacker may hold
        // by revoking every active refresh token for this user.
        var activeTokens = await refreshTokenRepo.GetActiveByUserAsync(user.Id, cancellationToken);
        if (activeTokens.Count > 0)
        {
            foreach (var token in activeTokens)
                token.IsRevoked = true;
            await refreshTokenRepo.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
