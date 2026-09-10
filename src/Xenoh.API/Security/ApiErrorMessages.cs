namespace Xenoh.API.Security;

/// <summary>Converts exception text into a bounded message that is safe to return to an API client.</summary>
public static class ApiErrorMessages
{
    private static readonly string[] TechnicalMarkers =
    [
        "exception", "stack trace", "npgsql", "entityframework", "microsoft.", "system.",
        "sql", " at ", "\\", "/src/", "/app/", "/api/", "http://", "https://", "node_modules", "0x"
    ];

    public static string Safe(string? message, string fallback)
    {
        var value = message?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 240)
            return fallback;
        if (value.Any(char.IsControl))
            return fallback;

        foreach (var marker in TechnicalMarkers)
        {
            if (value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return fallback;
        }

        return value;
    }
}
