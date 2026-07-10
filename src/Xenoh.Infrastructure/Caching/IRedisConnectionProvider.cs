using StackExchange.Redis;

namespace Xenoh.Infrastructure.Caching;

internal interface IRedisConnectionProvider : IDisposable
{
    bool IsAvailable { get; }
    IDatabase? GetDatabase();
    void BypassTemporarily();
}
