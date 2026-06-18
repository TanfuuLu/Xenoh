using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Xenoh.API.Security;

public static class ForwardedHeadersSetup
{
    public static void ConfigureTrustedForwardedHeaders(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;

        foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (IPAddress.TryParse(proxy, out var ipAddress))
                options.KnownProxies.Add(ipAddress);
        }

        foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
                continue;

            options.KnownIPNetworks.Add(ipNetwork);
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (!string.IsNullOrWhiteSpace(allowedHosts) && allowedHosts != "*")
        {
            foreach (var host in allowedHosts.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                options.AllowedHosts.Add(host);
        }
    }
}
