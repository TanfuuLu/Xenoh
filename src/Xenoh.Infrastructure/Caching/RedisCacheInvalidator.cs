using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisCacheInvalidator(
    IRedisConnectionProvider connectionProvider,
    RedisApplicationCache cache,
    ILogger<RedisCacheInvalidator> logger) : ICacheInvalidator
{
    public Task InvalidateAsync(string tag, CancellationToken cancellationToken = default) =>
        InvalidateAsync([tag], cancellationToken);

    public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var database = connectionProvider.GetDatabase();
        if (database is null)
            return;

        try
        {
            foreach (var tag in tags.Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await database.StringIncrementAsync(cache.Key($"tag:{tag}"));
                RedisMetrics.Invalidations.Inc();
            }
        }
        catch (RedisException ex)
        {
            connectionProvider.BypassTemporarily();
            logger.LogWarning(ex, "Redis cache invalidation failed for {Tags}.", string.Join(',', tags));
        }
    }
}
