using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using StackExchange.Redis;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Caching;

namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Redis is the fast path for live access-token revocation. PostgreSQL remains the
/// durable record and is used whenever Redis cannot safely answer the check.
/// </summary>
internal sealed class RedisTokenBlacklist(
    IApplicationDbContext db,
    IRedisConnectionProvider connectionProvider,
    IOptions<RedisOptions> options) : ITokenBlacklist
{
    private readonly RedisOptions _options = options.Value;

    public async Task RevokeTokenAsync(string token)
    {
        var expiresAt = GetExpiry(token);
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return;

        var database = connectionProvider.GetDatabase()
            ?? throw new InvalidOperationException("Redis is required to revoke an active access token.");

        var hash = ComputeHash(token);
        // Write Redis first: a database failure can only over-revoke, never leave a
        // revoked token usable by another API node.
        await database.StringSetAsync(RevocationKey(hash), "1", ttl);

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
        var database = connectionProvider.GetDatabase();
        if (database is not null)
        {
            var warmed = await database.StringGetAsync(WarmupKey);
            if (warmed.HasValue)
            {
                var revoked = await database.KeyExistsAsync(RevocationKey(hash));
                RedisMetrics.RevocationChecks.WithLabels(revoked ? "revoked" : "allowed").Inc();
                return revoked;
            }
        }

        var fallbackRevoked = await db.RevokedTokens
            .AsNoTracking()
            .AnyAsync(r => r.TokenHash == hash && r.ExpiresAt > DateTime.UtcNow);
        RedisMetrics.RevocationChecks.WithLabels(fallbackRevoked ? "database-revoked" : "database-allowed").Inc();
        return fallbackRevoked;
    }

    private string RevocationKey(string hash) => $"{_options.InstanceName}revoked-token:{hash}";
    private string WarmupKey => $"{_options.InstanceName}revoked-token:warmed";

    private static DateTime GetExpiry(string token)
    {
        try
        {
            var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
            return jwt.ValidTo == DateTime.MinValue ? DateTime.UtcNow.AddHours(1) : jwt.ValidTo;
        }
        catch
        {
            return DateTime.UtcNow.AddHours(1);
        }
    }

    internal static string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
