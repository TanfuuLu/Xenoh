using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xenoh.Infrastructure.Identity;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Infrastructure;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void HashRefreshToken_ReturnsStableHashWithoutPersistingRawToken()
    {
        var tokenService = CreateTokenService();
        const string rawRefreshToken = "raw-refresh-token-value";

        var firstHash = tokenService.HashRefreshToken(rawRefreshToken);
        var secondHash = tokenService.HashRefreshToken(rawRefreshToken);

        firstHash.Should().Be(secondHash);
        firstHash.Should().NotBe(rawRefreshToken);
        firstHash.Should().HaveLength(64);
        firstHash.All(c => char.IsDigit(c) || c is >= 'A' and <= 'F').Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_RejectsSpoofedImageContent()
    {
        var uploadCalled = false;
        var storage = new UserAvatarStorageService(
            (_, _) =>
            {
                uploadCalled = true;
                return Task.FromResult(new PutObjectResponse());
            },
            CreateR2AvatarOptions());
        await using var stream = new MemoryStream("not actually a png"u8.ToArray());

        var act = () => storage.SaveAsync(
            Guid.NewGuid(),
            "avatar.png",
            "image/png",
            stream,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Avatar image content is invalid.");
        uploadCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AcceptsValidPngAndUsesGeneratedR2Key()
    {
        PutObjectRequest? capturedRequest = null;
        var storage = new UserAvatarStorageService(
            (request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new PutObjectResponse());
            },
            CreateR2AvatarOptions());
        var userId = Guid.NewGuid();
        await using var stream = new MemoryStream(
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ]);

        var url = await storage.SaveAsync(
            userId,
            "../../../avatar.exe",
            "image/png",
            stream,
            CancellationToken.None);

        url.Should().StartWith($"https://avatars.example.com/users-avatar/{userId:N}-");
        url.Should().EndWith(".png");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("user-avatar");
        capturedRequest.Key.Should().StartWith($"users-avatar/{userId:N}-");
        capturedRequest.Key.Should().EndWith(".png");
        capturedRequest.Key.Should().NotContain("avatar.exe");
        capturedRequest.ContentType.Should().Be("image/png");
        capturedRequest.Headers.CacheControl.Should().Be("public, max-age=31536000, immutable");
    }

    private static TokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-secret-key-at-least-32-characters",
                ["Jwt:Issuer"] = "XenohAPI",
                ["Jwt:Audience"] = "XenohClient",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        return new TokenService(configuration);
    }

    private static IOptions<R2AvatarOptions> CreateR2AvatarOptions() =>
        Options.Create(new R2AvatarOptions
        {
            AccountId = "test-account-id",
            BucketName = "user-avatar",
            AccessKeyId = "test-access-key",
            SecretAccessKey = "test-secret-key",
            PublicBaseUrl = "https://avatars.example.com"
        });
}
