using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisDistributedLock(
    IRedisConnectionProvider connectionProvider,
    IOptions<RedisOptions> options) : IDistributedLock
{
    private readonly RedisOptions _options = options.Value;

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string name,
        TimeSpan leaseTime,
        CancellationToken cancellationToken = default)
    {
        var database = connectionProvider.GetDatabase();
        if (database is null)
            return null;

        var key = $"{_options.InstanceName}lock:{name}";
        var token = Guid.NewGuid().ToString("N");
        var acquired = await database.StringSetAsync(key, token, leaseTime, When.NotExists);
        RedisMetrics.Locks.WithLabels(acquired ? "acquired" : "contended").Inc();
        return acquired ? new Lease(database, key, token) : null;
    }

    private sealed class Lease(IDatabase database, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        private const string ReleaseScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) end return 0";

        public async ValueTask DisposeAsync()
        {
            await database.ScriptEvaluateAsync(ReleaseScript, [key], [token]);
        }
    }
}
