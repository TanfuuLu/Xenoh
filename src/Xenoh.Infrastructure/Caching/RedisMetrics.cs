using Prometheus;

namespace Xenoh.Infrastructure.Caching;

internal static class RedisMetrics
{
    internal static readonly Counter CacheOperations = Metrics.CreateCounter(
        "xenoh_redis_cache_operations_total", "Redis cache operations by result.", ["result"]);
    internal static readonly Counter Invalidations = Metrics.CreateCounter(
        "xenoh_redis_cache_invalidations_total", "Redis cache tag invalidations.");
    internal static readonly Counter Locks = Metrics.CreateCounter(
        "xenoh_redis_locks_total", "Redis distributed lock attempts by result.", ["result"]);
    internal static readonly Counter RateLimits = Metrics.CreateCounter(
        "xenoh_redis_rate_limit_requests_total", "Redis rate-limit requests by result.", ["result"]);
    internal static readonly Counter RevocationChecks = Metrics.CreateCounter(
        "xenoh_redis_revocation_checks_total", "Access-token revocation checks by result.", ["result"]);
}
