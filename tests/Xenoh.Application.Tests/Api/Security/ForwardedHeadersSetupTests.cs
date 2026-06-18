using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Xenoh.API.Security;
using Xunit;

namespace Xenoh.Application.Tests.Api.Security;

public sealed class ForwardedHeadersSetupTests
{
    [Fact]
    public void ConfigureTrustedForwardedHeaders_IncludesForwardedHostForOAuthRedirects()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "xenoh.online,www.xenoh.online,api.xenoh.online",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1"
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        ForwardedHeadersSetup.ConfigureTrustedForwardedHeaders(options, configuration);

        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedFor);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedHost);
        options.KnownProxies.Should().Contain(ip => ip.ToString() == "127.0.0.1");
        options.AllowedHosts.Should().Contain(["xenoh.online", "www.xenoh.online", "api.xenoh.online"]);
    }

    [Fact]
    public void ConfigureTrustedForwardedHeaders_AddsConfiguredKnownNetworks()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "*",
                ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8"
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        ForwardedHeadersSetup.ConfigureTrustedForwardedHeaders(options, configuration);

        options.KnownIPNetworks.Should().Contain(ip => ip.ToString() == "10.0.0.0/8");
        options.AllowedHosts.Should().BeEmpty();
    }
}
