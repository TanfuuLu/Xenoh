using System.Security.Cryptography;
using System.Text;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

internal static class ExternalAuthTicketHasher
{
    public static string Hash(string ticket)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
        return Convert.ToHexString(bytes);
    }
}
