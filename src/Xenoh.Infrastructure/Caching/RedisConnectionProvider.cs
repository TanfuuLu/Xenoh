using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Xenoh.Infrastructure.Caching;

internal sealed class RedisConnectionProvider : IRedisConnectionProvider
{
    private readonly RedisOptions _options;
    private readonly ILogger<RedisConnectionProvider> _logger;
    private readonly Lazy<IConnectionMultiplexer?> _connection;
    private long _bypassUntilTicks;

    public RedisConnectionProvider(IOptions<RedisOptions> options, ILogger<RedisConnectionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connection = new Lazy<IConnectionMultiplexer?>(Connect);
    }

    public bool IsAvailable =>
        _options.Enabled &&
        DateTime.UtcNow.Ticks >= Interlocked.Read(ref _bypassUntilTicks) &&
        _connection.Value?.IsConnected == true;

    public IDatabase? GetDatabase() => IsAvailable ? _connection.Value!.GetDatabase() : null;

    public void BypassTemporarily() =>
        Interlocked.Exchange(ref _bypassUntilTicks, DateTime.UtcNow.AddSeconds(15).Ticks);

    private IConnectionMultiplexer? Connect()
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ConnectionString))
            return null;

        try
        {
            var configuration = ConfigurationOptions.Parse(RedisConnectionString.Normalize(_options.ConnectionString));
            configuration.AbortOnConnectFail = false;
            configuration.ConnectTimeout = _options.ConnectTimeoutMilliseconds;
            configuration.SyncTimeout = _options.SyncTimeoutMilliseconds;
            configuration.ClientName = "xenoh-api";

            var connection = ConnectionMultiplexer.Connect(configuration);
            connection.ConnectionFailed += (_, args) =>
                _logger.LogWarning("Redis connection failed for endpoint {Endpoint}: {FailureType}", args.EndPoint, args.FailureType);
            connection.ConnectionRestored += (_, args) =>
                _logger.LogInformation("Redis connection restored for endpoint {Endpoint}", args.EndPoint);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis is unavailable; cache-backed reads will use PostgreSQL.");
            return null;
        }
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
            _connection.Value?.Dispose();
    }
}
