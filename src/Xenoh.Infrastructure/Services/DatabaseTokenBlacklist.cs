using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Services;

public sealed class DatabaseTokenBlacklist(IApplicationDbContext db) : ITokenBlacklist
{
    public async Task RevokeTokenAsync(string token)
    {
        DateTime expiresAt;
        try
        {
            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(token);
            expiresAt = jwt.ValidTo == DateTime.MinValue
                ? DateTime.UtcNow.AddHours(1)
                : jwt.ValidTo;
        }
        catch
        {
            expiresAt = DateTime.UtcNow.AddHours(1);
        }

        var hash = ComputeHash(token);

        var exists = await db.RevokedTokens.AnyAsync(r => r.TokenHash == hash);
        if (!exists)
        {
            db.RevokedTokens.Add(new RevokedToken { TokenHash = hash, ExpiresAt = expiresAt });
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        var hash = ComputeHash(token);
        return await db.RevokedTokens
            .AsNoTracking()
            .AnyAsync(r => r.TokenHash == hash && r.ExpiresAt > DateTime.UtcNow);
    }

    private static string ComputeHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
