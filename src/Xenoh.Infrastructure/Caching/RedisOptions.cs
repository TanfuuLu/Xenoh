namespace Xenoh.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
    public string InstanceName { get; init; } = "xenoh:";
    public int ConnectTimeoutMilliseconds { get; init; } = 1_000;
    public int SyncTimeoutMilliseconds { get; init; } = 1_000;
}
