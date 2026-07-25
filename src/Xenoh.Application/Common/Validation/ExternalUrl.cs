namespace Xenoh.Application.Common.Validation;

/// <summary>
/// Validation for user-supplied links that other people's browsers will eventually render.
/// A stored <c>javascript:</c> value becomes cross-user XSS the moment it reaches an
/// <c>href</c>, so the scheme is pinned here, at the write, rather than trusted from the client.
/// </summary>
public static class ExternalUrl
{
    /// <summary>
    /// Trims the value and returns null when blank. Throws when the value is present but is not
    /// an absolute http(s) URL.
    /// </summary>
    public static string? NormalizeOrThrow(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException($"{fieldName} must be an http:// or https:// URL.");

        return trimmed;
    }
}
