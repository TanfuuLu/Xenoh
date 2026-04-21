using System.Collections.Concurrent;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class InMemoryTokenBlacklist : ITokenBlacklist
{
    private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public void RevokeToken(string token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _revokedTokens[token] = DateTime.UtcNow;
        }
    }

    public bool IsTokenRevoked(string token)
    {
        return _revokedTokens.ContainsKey(token);
    }

    // Optional: Clean up expired revoked tokens periodically
    public void CleanupExpiredTokens(TimeSpan retentionPeriod)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(retentionPeriod);
        foreach (var kvp in _revokedTokens)
        {
            if (kvp.Value < cutoffTime)
            {
                _revokedTokens.TryRemove(kvp.Key, out _);
            }
        }
    }
}
