using System.Security.Cryptography;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ResetPasswordWithCodeHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db
) : IRequestHandler<ResetPasswordWithCodeCommand>
{
    private const int MaxFailedAttempts = 5;

    public async ValueTask<Unit> Handle(ResetPasswordWithCodeCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new InvalidOperationException("Invalid or expired reset code.");

        var now = DateTime.UtcNow;
        var resetCode = await db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Invalid or expired reset code.");

        if (resetCode.ExpiresAt <= now || resetCode.FailedAttempts >= MaxFailedAttempts)
            throw new InvalidOperationException("Invalid or expired reset code.");

        var expectedHash = PasswordResetCodeHasher.Hash(user.Email!, request.Code, user);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(resetCode.CodeHash),
                Convert.FromHexString(expectedHash)))
        {
            resetCode.FailedAttempts++;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Invalid or expired reset code.");
        }

        var result = await userManager.ResetPasswordAsync(user, resetCode.ResetToken, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        resetCode.UsedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
