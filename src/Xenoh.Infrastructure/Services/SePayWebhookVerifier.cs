using System.Security.Cryptography;
using System.Text;
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

        var expectedKey = options.Value.ApiKey;
        if (string.IsNullOrEmpty(expectedKey)) return false;

        var providedKey = authorizationHeader[prefix.Length..].Trim();

        // Constant-time comparison so the response time cannot leak how many leading
        // bytes of the API key were guessed correctly.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(expectedKey));
    }
}
