using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeApplicationCache : IApplicationCache
{
    private readonly Dictionary<string, object> _entries = [];

    public int FactoryCalls { get; private set; }

    public async Task<T> GetOrCreateAsync<T>(
        string tag,
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var entryKey = $"{tag}|{key}";
        if (_entries.TryGetValue(entryKey, out var entry))
            return (T)entry;

        FactoryCalls++;
        var value = await factory(cancellationToken);
        _entries[entryKey] = value!;
        return value;
    }
}
