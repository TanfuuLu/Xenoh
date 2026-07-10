namespace Xenoh.Infrastructure.Caching;

/// <summary>Normalizes provider URLs (for example Upstash rediss:// URLs) to the
/// comma-delimited format expected by StackExchange.Redis.</summary>
public static class RedisConnectionString
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            return connectionString;

        if (!string.Equals(uri.Scheme, "redis", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, "rediss", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Redis connection URLs must use redis:// or rediss://, not an HTTP URL.",
                nameof(connectionString));
        }

        var parts = new List<string> { $"{uri.Host}:{uri.Port}" };
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length == 2)
        {
            if (!string.IsNullOrWhiteSpace(userInfo[0]))
                parts.Add($"user={Uri.UnescapeDataString(userInfo[0])}");
            parts.Add($"password={Uri.UnescapeDataString(userInfo[1])}");
        }
        else if (userInfo.Length == 1 && !string.IsNullOrWhiteSpace(userInfo[0]))
        {
            parts.Add($"password={Uri.UnescapeDataString(userInfo[0])}");
        }

        parts.Add($"ssl={uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase)}");
        return string.Join(',', parts);
    }
}
