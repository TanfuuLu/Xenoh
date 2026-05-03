using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class SePayWebhookVerifier(IOptions<SePayOptions> options) : ISePayWebhookVerifier
{
    public bool Verify(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)) return false;

        const string prefix = "Apikey ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var providedKey = authorizationHeader[prefix.Length..].Trim();
        return string.Equals(providedKey, options.Value.ApiKey, StringComparison.Ordinal);
    }
}
