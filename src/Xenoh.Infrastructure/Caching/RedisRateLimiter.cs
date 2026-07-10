using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisRateLimiter(
    IRedisConnectionProvider connectionProvider,
    IOptions<RedisOptions> options,
    IConfiguration configuration) : IRedisRateLimiter
{
    private const string IncrementScript = "local count = redis.call('INCR', KEYS[1]); if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end; return count";
    private readonly RedisOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled;

    public async Task<RedisRateLimitLease> AcquireAsync(
        string policy, string partitionKey, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return new(true, true, TimeSpan.Zero);

        var database = connectionProvider.GetDatabase();
        if (database is null)
        {
            RedisMetrics.RateLimits.WithLabels("unavailable").Inc();
            return new(false, false, TimeSpan.Zero);
        }

        var now = DateTimeOffset.UtcNow;
        var retryAfter = TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds(now.Second) - TimeSpan.FromMilliseconds(now.Millisecond);
        var window = now.ToUnixTimeSeconds() / 60;
        var limit = GetLimit(policy);
        var key = $"{_options.InstanceName}rate-limit:{policy}:{window}:{partitionKey}";

        try
        {
            var count = (long)await database.ScriptEvaluateAsync(IncrementScript, [key], [(long)TimeSpan.FromMinutes(1).TotalMilliseconds]);
            var allowed = count <= limit;
            RedisMetrics.RateLimits.WithLabels(allowed ? "allowed" : "rejected").Inc();
            return new(allowed, true, retryAfter);
        }
        catch (RedisException)
        {
            RedisMetrics.RateLimits.WithLabels("unavailable").Inc();
            return new(false, false, TimeSpan.Zero);
        }
    }

    private int GetLimit(string policy) => policy switch
    {
        "global" => Get("RateLimiting:Global:PermitLimit", 300),
        "auth" => Get("RateLimiting:Auth:PermitLimit", 10),
        "refresh-token" => Get("RateLimiting:Auth:PermitLimit", 20),
        "external-auth" => Get("RateLimiting:Auth:PermitLimit", 10),
        "ai" => Get("RateLimiting:Ai:PermitLimit", 30),
        "webhook" => Get("RateLimiting:Webhook:PermitLimit", 20),
        "public-share" => 60,
        _ => Get("RateLimiting:Global:PermitLimit", 300)
    };

    private int Get(string key, int fallback) => configuration.GetValue<int?>(key) is > 0 ? configuration.GetValue<int>(key) : fallback;
}
