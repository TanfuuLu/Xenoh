using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisApplicationCache(
    IRedisConnectionProvider connectionProvider,
    IOptions<RedisOptions> options,
    ILogger<RedisApplicationCache> logger) : IApplicationCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisOptions _options = options.Value;

    public async Task<T> GetOrCreateAsync<T>(
        string tag,
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var database = connectionProvider.GetDatabase();
        if (database is null)
            return await factory(cancellationToken);

        try
        {
            var version = await GetTagVersionAsync(database, tag);
            var cacheKey = Key($"cache:{tag}:v{version}:{key}");
            var cached = await database.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                var value = JsonSerializer.Deserialize<T>(cached.ToString(), JsonOptions);
                if (value is not null)
                {
                    RedisMetrics.CacheOperations.WithLabels("hit").Inc();
                    return value;
                }
            }

            RedisMetrics.CacheOperations.WithLabels("miss").Inc();
            var result = await factory(cancellationToken);
            var serialized = JsonSerializer.Serialize(result, JsonOptions);
            await database.StringSetAsync(cacheKey, serialized, ttl);
            return result;
        }
        catch (RedisException ex)
        {
            RedisMetrics.CacheOperations.WithLabels("error").Inc();
            connectionProvider.BypassTemporarily();
            logger.LogWarning(ex, "Redis cache operation failed for tag {Tag}; using PostgreSQL.", tag);
            return await factory(cancellationToken);
        }
    }

    internal async Task<long> GetTagVersionAsync(IDatabase database, string tag)
    {
        var tagKey = Key($"tag:{tag}");
        var current = await database.StringGetAsync(tagKey);
        if (current.HasValue && long.TryParse(current.ToString(), out var version))
            return version;

        await database.StringSetAsync(tagKey, 1, when: When.NotExists);
        current = await database.StringGetAsync(tagKey);
        return current.HasValue && long.TryParse(current.ToString(), out version) ? version : 1;
    }

    internal string Key(string suffix) => $"{_options.InstanceName}{suffix}";
}
