using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Xenoh.API.Auth;
using Xenoh.API.Controllers;
using Xenoh.API.Security;
using Xenoh.Infrastructure.Identity;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Infrastructure;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void ValidateRequiredConfiguration_RejectsProductionPlaceholders()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost",
                ["Jwt:Key"] = "CHANGE_ME_MINIMUM_32_CHAR_RANDOM_SECRET",
                ["Jwt:Issuer"] = "XenohAPI",
                ["Jwt:Audience"] = "XenohClient",
                ["Smtp:Host"] = "smtp.example.com",
                ["Smtp:Username"] = "noreply@example.com",
                ["Smtp:Password"] = "CHANGE_ME",
                ["Authentication:FrontendUrl"] = "https://www.example.com",
                ["Authentication:Google:ClientId"] = "CHANGE_ME",
                ["Authentication:Google:ClientSecret"] = "CHANGE_ME",
                ["Authentication:Facebook:AppId"] = "CHANGE_ME",
                ["Authentication:Facebook:AppSecret"] = "CHANGE_ME",
                ["SePay:ApiKey"] = "CHANGE_ME",
                ["OpenAi:ApiKey"] = "CHANGE_ME",
                ["R2Avatar:AccountId"] = "CHANGE_ME",
                ["R2Avatar:BucketName"] = "user-avatar",
                ["R2Avatar:AccessKeyId"] = "CHANGE_ME",
                ["R2Avatar:SecretAccessKey"] = "CHANGE_ME",
                ["R2Avatar:PublicBaseUrl"] = "https://assets.example.com",
                ["R2Share:AccountId"] = "CHANGE_ME",
                ["R2Share:BucketName"] = "xenoh-bucket",
                ["R2Share:AccessKeyId"] = "CHANGE_ME",
                ["R2Share:SecretAccessKey"] = "CHANGE_ME",
                ["R2Share:PublicBaseUrl"] = "https://assets.example.com"
            })
            .Build();

        var act = () => ExternalAuthHelpers.ValidateRequiredConfiguration(
            configuration,
            new FakeWebHostEnvironment { EnvironmentName = "Production" });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Missing or placeholder production configuration:*");
    }

    [Fact]
    public void ConfigureTrustedForwardedHeaders_AddsConfiguredKnownProxiesWithoutClearingDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "172.18.0.1"
            })
            .Build();
        var options = new ForwardedHeadersOptions();
        var existingProxyCount = options.KnownProxies.Count;

        ForwardedHeadersSetup.ConfigureTrustedForwardedHeaders(options, configuration);

        options.ForwardedHeaders.Should().Be(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);
        options.KnownProxies.Should().Contain(IPAddress.Parse("172.18.0.1"));
        options.KnownProxies.Count.Should().BeGreaterThan(existingProxyCount);
    }

    [Fact]
    public void RateLimitPartitionKey_PrefersAuthenticatedUserForUserScopedLimits()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        ], "Test"));

        RateLimitingSetup.GetUserOrIpPartitionKey(context).Should().Be("user:user-123");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_AddsProductionHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/users/me";
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(
            async _ =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("{}");
            },
            new FakeWebHostEnvironment { EnvironmentName = "Production" });

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Strict-Transport-Security"].ToString().Should().Contain("max-age=31536000");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public void HighRiskEndpoints_DeclareExpectedRateLimitPolicies()
    {
        ControllerAction<AuthController>(nameof(AuthController.RefreshToken))
            .Should().ContainSingle(a => a.PolicyName == RateLimitPolicyNames.RefreshToken);
        ControllerAction<AuthController>(nameof(AuthController.ExternalLogin))
            .Should().ContainSingle(a => a.PolicyName == RateLimitPolicyNames.ExternalAuth);
        ControllerAction<AuthController>(nameof(AuthController.ExchangeExternalLoginTicket))
            .Should().ContainSingle(a => a.PolicyName == RateLimitPolicyNames.ExternalAuth);
        ControllerAction<PaymentsController>(nameof(PaymentsController.SePayWebhook))
            .Should().ContainSingle(a => a.PolicyName == RateLimitPolicyNames.Webhook);
        ControllerAction<ShareController>(nameof(ShareController.GetShareImage))
            .Should().ContainSingle(a => a.PolicyName == RateLimitPolicyNames.PublicShare);
    }

    [Fact]
    public void Source_DoesNotUseUnsafeRawSqlOutsideMigrations()
    {
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root, "src");
        var trustedMaintenanceSeed = Path.Combine(
            sourceRoot,
            "Xenoh.Infrastructure",
            "Persistence",
            "Seeders",
            "DatabaseInitializer.cs");
        const string trustedCommandAssignment = "command.CommandText = sql;";
        var blockedPatterns = new[]
        {
            "FromSqlRaw",
            "ExecuteSqlRaw",
            "SqlQueryRaw",
            "NpgsqlCommand",
            "DbCommand",
            "CommandText"
        };

        var sourceLines = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, line, lineNumber = index + 1 }))
            .ToArray();
        sourceLines
            .Where(entry =>
                string.Equals(entry.path, trustedMaintenanceSeed, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.line.Trim(), trustedCommandAssignment, StringComparison.Ordinal))
            .Should()
            .ContainSingle("the Development-only embedded seed must remain the sole reviewed raw-SQL assignment");

        var offenders = sourceLines
            .Where(entry => blockedPatterns.Any(pattern =>
                entry.line.Contains(pattern, StringComparison.Ordinal)) &&
                !(string.Equals(entry.path, trustedMaintenanceSeed, StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(entry.line.Trim(), trustedCommandAssignment, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.lineNumber}")
            .ToArray();

        offenders.Should().BeEmpty();
    }

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

    private static IEnumerable<EnableRateLimitingAttribute> ControllerAction<TController>(string actionName) =>
        typeof(TController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName)
            .GetCustomAttributes<EnableRateLimitingAttribute>();

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "Xenoh.API")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Xenoh_be repository root.");
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Xenoh.API";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
