using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Xenoh.API.Auth;
using Xenoh.API.Controllers;
using Xunit;

namespace Xenoh.Application.Tests.Api.Auth;

public sealed class ExternalAuthReturnTargetTests
{
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:FrontendUrl"] = "https://xenoh.online",
            ["Authentication:MobileCallbackUrl"] = "xenoh://auth/social-callback"
        })
        .Build();

    [Fact]
    public void TrySetReturnTarget_MissingClient_PreservesWebFlow()
    {
        var properties = new AuthenticationProperties();

        var accepted = ExternalAuthReturnTargets.TrySetReturnTarget(properties, null);

        accepted.Should().BeTrue();
        properties.Items.Should().NotContainKey(ExternalAuthReturnTargets.ReturnTargetItemKey);
        ExternalAuthReturnTargets.BuildSuccessRedirect(_configuration, properties, "ticket value")
            .Should().Be("https://xenoh.online/auth/social-callback?ticket=ticket%20value");
    }

    [Fact]
    public void TrySetReturnTarget_MobileClient_UsesOnlyConfiguredMobileCallback()
    {
        var properties = new AuthenticationProperties();

        var accepted = ExternalAuthReturnTargets.TrySetReturnTarget(properties, "mobile");

        accepted.Should().BeTrue();
        properties.Items[ExternalAuthReturnTargets.ReturnTargetItemKey].Should().Be("mobile");
        ExternalAuthReturnTargets.BuildSuccessRedirect(_configuration, properties, "abc+/=")
            .Should().Be("xenoh://auth/social-callback?ticket=abc%2B%2F%3D");
    }

    [Theory]
    [InlineData("")]
    [InlineData("web")]
    [InlineData("https://attacker.example/callback")]
    [InlineData("mobile&returnUrl=https://attacker.example")]
    public void TrySetReturnTarget_UnsupportedClient_IsRejected(string client)
    {
        var properties = new AuthenticationProperties();

        ExternalAuthReturnTargets.TrySetReturnTarget(properties, client).Should().BeFalse();
        properties.Items.Should().NotContainKey(ExternalAuthReturnTargets.ReturnTargetItemKey);
    }

    [Fact]
    public void BuildFailureRedirect_MobileFlow_UsesStableEncodedErrorCode()
    {
        var properties = new AuthenticationProperties();
        ExternalAuthReturnTargets.TrySetReturnTarget(properties, "mobile");

        ExternalAuthReturnTargets.BuildFailureRedirect(_configuration, properties)
            .Should().Be("xenoh://auth/social-callback?error=external_login_failed");
    }

    [Fact]
    public void BuildFailureRedirect_WebFlow_PreservesExistingContract()
    {
        ExternalAuthReturnTargets.BuildFailureRedirect(
                _configuration,
                new AuthenticationProperties())
            .Should().Be("https://xenoh.online/login?externalError=External%20login%20failed.");
    }

    [Fact]
    public void ExternalLogin_MobileClient_PersistsFixedReturnTargetInChallengeProperties()
    {
        var controller = new AuthController(null!, NullLogger<AuthController>.Instance);

        var result = controller.ExternalLogin("google", "mobile");

        var challenge = result.Should().BeOfType<ChallengeResult>().Subject;
        challenge.Properties!.Items[ExternalAuthReturnTargets.ReturnTargetItemKey]
            .Should().Be("mobile");
    }

    [Fact]
    public void ExternalLogin_ArbitraryClient_IsRejectedBeforeOAuthChallenge()
    {
        var controller = new AuthController(null!, NullLogger<AuthController>.Instance);

        controller.ExternalLogin("facebook", "https://attacker.example")
            .Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://xenoh.online/auth/social-callback")]
    [InlineData("xenoh://other/social-callback")]
    [InlineData("xenoh://auth/other")]
    [InlineData("xenoh://auth/social-callback?ticket=seeded")]
    public void IsValidMobileCallback_RejectsMissingOrUnexpectedCallbacks(string? callback)
    {
        ExternalAuthReturnTargets.IsValidMobileCallback(callback).Should().BeFalse();
    }

    [Fact]
    public void IsValidMobileCallback_AcceptsExactApplicationCallback()
    {
        ExternalAuthReturnTargets.IsValidMobileCallback("xenoh://auth/social-callback")
            .Should().BeTrue();
    }
}
