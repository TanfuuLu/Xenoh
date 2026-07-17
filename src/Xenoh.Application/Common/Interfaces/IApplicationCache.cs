namespace Xenoh.Application.Common.Interfaces;

/// <summary>
/// Distributed cache for read models. Callers provide a resource tag so writes can
/// invalidate related entries without scanning a Redis keyspace.
/// </summary>
public interface IApplicationCache
{
    Task<T> GetOrCreateAsync<T>(
        string tag,
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}

public interface ICacheInvalidator
{
    Task InvalidateAsync(string tag, CancellationToken cancellationToken = default);
    Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string name,
        TimeSpan leaseTime,
        CancellationToken cancellationToken = default);
}

public interface IRedisRateLimiter
{
    bool IsEnabled { get; }
    Task<RedisRateLimitLease> AcquireAsync(string policy, string partitionKey, CancellationToken cancellationToken = default);
}

public sealed record RedisRateLimitLease(bool IsAllowed, bool IsAvailable, TimeSpan RetryAfter);

public static class CacheTags
{
    public const string Leaderboards = "leaderboards";
    public const string Admin = "admin";
    public const string CoachDashboards = "coach-dashboards";
    public const string Foods = "foods";

    public static string User(Guid userId) => $"user:{userId:N}";
    public static string Coach(Guid coachId) => $"coach:{coachId:N}";
}
