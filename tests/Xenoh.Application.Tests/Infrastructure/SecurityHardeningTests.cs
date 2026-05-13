using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
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
        using var tempRoot = new TempDirectory();
        var storage = new UserAvatarStorageService(new TestWebHostEnvironment(tempRoot.Path));
        await using var stream = new MemoryStream("not actually a png"u8.ToArray());

        var act = () => storage.SaveAsync(
            Guid.NewGuid(),
            "avatar.png",
            "image/png",
            stream,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Avatar image content is invalid.");
        Directory.Exists(System.IO.Path.Combine(tempRoot.Path, "uploads", "users-avatar"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AcceptsValidPngAndUsesGeneratedFileName()
    {
        using var tempRoot = new TempDirectory();
        var storage = new UserAvatarStorageService(new TestWebHostEnvironment(tempRoot.Path));
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

        url.Should().StartWith($"/uploads/users-avatar/{userId:N}-");
        url.Should().EndWith(".png");

        var savedPath = System.IO.Path.Combine(
            tempRoot.Path,
            url.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
        File.Exists(savedPath).Should().BeTrue();
        System.IO.Path.GetFileName(savedPath).Should().NotContain("avatar.exe");
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

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Xenoh.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = webRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"xenoh-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
