using System.Collections.Concurrent;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

/// <summary>
/// In-memory <see cref="ITokenBlacklist"/> for tests: records revoked tokens in a set so
/// assertions can check whether a given access token was blacklisted.
/// </summary>
public sealed class FakeTokenBlacklist : ITokenBlacklist
{
    private readonly ConcurrentDictionary<string, byte> _revoked = new();

    public Task RevokeTokenAsync(string token)
    {
        _revoked.TryAdd(token, 0);
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenRevokedAsync(string token) =>
        Task.FromResult(_revoked.ContainsKey(token));

    public bool WasRevoked(string token) => _revoked.ContainsKey(token);
}
