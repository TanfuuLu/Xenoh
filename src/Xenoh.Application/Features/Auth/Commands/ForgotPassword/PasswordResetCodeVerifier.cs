using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

internal static class PasswordResetCodeVerifier
{
    private const int MaxFailedAttempts = 5;
    private const string InvalidResetCodeMessage = "Invalid or expired reset code.";

    public static async Task<PasswordResetCode> VerifyAsync(
        ApplicationUser user,
        string code,
        IApplicationDbContext db,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var resetCode = await db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(InvalidResetCodeMessage);

        if (resetCode.ExpiresAt <= now || resetCode.FailedAttempts >= MaxFailedAttempts)
            throw new InvalidOperationException(InvalidResetCodeMessage);

        var expectedHash = PasswordResetCodeHasher.Hash(user.Email!, code, user);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(resetCode.CodeHash),
                Convert.FromHexString(expectedHash)))
        {
            resetCode.FailedAttempts++;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(InvalidResetCodeMessage);
        }

        return resetCode;
    }
}
