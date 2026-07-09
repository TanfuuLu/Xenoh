using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Application.Tests.Infrastructure;

public sealed class SePayWebhookVerifierTests
{
    private const string ApiKey = "xenoh_api_sepay-payment-webhook-key";

    private static SePayWebhookVerifier CreateVerifier(string apiKey = ApiKey) =>
        new(Options.Create(new SePayOptions { ApiKey = apiKey }));

    [Fact]
    public void Verify_WithCorrectKey_ReturnsTrue() =>
        CreateVerifier().Verify($"Apikey {ApiKey}").Should().BeTrue();

    [Fact]
    public void Verify_WithCorrectKeyCaseInsensitivePrefix_ReturnsTrue() =>
        CreateVerifier().Verify($"apikey {ApiKey}").Should().BeTrue();

    [Theory]
    [InlineData("Apikey wrong-key")]
    [InlineData("Apikey ")]
    [InlineData("Bearer " + ApiKey)]
    [InlineData(ApiKey)]
    [InlineData("")]
    [InlineData(null)]
    public void Verify_WithInvalidHeader_ReturnsFalse(string? header) =>
        CreateVerifier().Verify(header).Should().BeFalse();

    [Fact]
    public void Verify_WhenConfiguredKeyEmpty_ReturnsFalse() =>
        CreateVerifier(apiKey: "").Verify("Apikey ").Should().BeFalse();
}
