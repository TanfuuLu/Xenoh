using System.Security.Cryptography;
using System.Text;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

internal static class PasswordResetCodeHasher
{
    public static string Hash(string email, string code, ApplicationUser user)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var input = $"{normalizedEmail}:{code.Trim()}:{user.SecurityStamp}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
