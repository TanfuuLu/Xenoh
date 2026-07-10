using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisRevocationWarmupService(
    IServiceScopeFactory scopeFactory,
    IRedisConnectionProvider connectionProvider,
    IOptions<RedisOptions> options,
    ILogger<RedisRevocationWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = connectionProvider.GetDatabase();
        if (database is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var revoked = await db.RevokedTokens
            .AsNoTracking()
            .Where(token => token.ExpiresAt > now)
            .Select(token => new { token.TokenHash, token.ExpiresAt })
            .ToListAsync(cancellationToken);

        var prefix = options.Value.InstanceName;
        foreach (var token in revoked)
            await database.StringSetAsync($"{prefix}revoked-token:{token.TokenHash}", "1", token.ExpiresAt - now);

        // The marker makes Redis a safe fast path after every existing database
        // revocation has been copied. It expires with the longest possible token.
        await database.StringSetAsync($"{prefix}revoked-token:warmed", "1", TimeSpan.FromHours(2));
        logger.LogInformation("Redis token blacklist warmed with {Count} active revocations.", revoked.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
