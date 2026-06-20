using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;

namespace Xenoh.API.Auth;

internal static class ExternalAuthHelpers
{
    internal static OAuthEvents CreateExternalAuthEvents(string provider, IConfiguration configuration)
    {
        return new OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                // The redirect_uri must satisfy two constraints:
                //   1. Its HOST must match the origin the browser used to start the flow,
                //      otherwise the OAuth correlation cookie (set on that origin) is not
                //      sent on the callback and the handshake fails.
                //   2. Its SCHEME must be the public https scheme registered with the
                //      provider. Behind a reverse proxy, request.Scheme arrives as http
                //      whenever X-Forwarded-Proto is not trusted (e.g. the proxy IP is not
                //      in ForwardedHeaders:KnownProxies). That previously produced an
                //      http:// redirect_uri which Google and Facebook reject outright
                //      ("Access blocked" / "URL Blocked").
                // So we take the host from the live request but pin the scheme to the
                // configured public BackendUrl, which is the value registered with the
                // providers. We fall back to BackendUrl entirely if the request host is
                // unavailable.
                var request = context.HttpContext.Request;
                var backendUrl = configuration["Authentication:BackendUrl"]?.TrimEnd('/');
                var publicScheme = Uri.TryCreate(backendUrl, UriKind.Absolute, out var backendUri)
                    ? backendUri.Scheme
                    : request.Scheme;
                var callbackOrigin = request.Host.HasValue
                    ? $"{publicScheme}://{request.Host.Value}"
                    : backendUrl;
                var callbackPath = context.Options.CallbackPath.Value;
                if (!string.IsNullOrWhiteSpace(callbackOrigin) && !string.IsNullOrWhiteSpace(callbackPath))
                {
                    var publicCallbackUrl = $"{callbackOrigin.TrimEnd('/')}{callbackPath}";
                    context.RedirectUri = ReplaceQueryValue(context.RedirectUri, "redirect_uri", publicCallbackUrl);
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnTicketReceived = async context =>
            {
                var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
                var principal = context.Principal ?? throw new InvalidOperationException("External login principal was not returned.");
                var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("External provider did not return a user identifier.");
                var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? throw new InvalidOperationException("External provider did not return an email address.");
                var fullName = principal.FindFirstValue(ClaimTypes.Name);
                var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
                var lastName = principal.FindFirstValue(ClaimTypes.Surname);
                if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(fullName))
                {
                    var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    firstName = nameParts.ElementAtOrDefault(0);
                    lastName = nameParts.ElementAtOrDefault(1);
                }

                var ticket = await mediator.Send(new ExternalLoginCommand(
                    provider,
                    providerKey,
                    email,
                    firstName,
                    lastName,
                    principal.FindFirstValue("picture")
                ), context.HttpContext.RequestAborted);

                var redirectUrl = BuildFrontendRedirectUrl(configuration, "auth/social-callback", ("ticket", ticket.Ticket));
                await context.HttpContext.SignOutAsync("External");
                context.Response.Redirect(redirectUrl);
                context.HandleResponse();
            },
            OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Xenoh.API.Auth.ExternalLogin");
                logger.LogError(
                    context.Failure,
                    "External login via {Provider} failed at the OAuth callback. Verify Authentication:BackendUrl ({BackendUrl}) matches the public API origin the browser used and that this callback URL is registered with the provider.",
                    provider,
                    configuration["Authentication:BackendUrl"]);

                var redirectUrl = BuildFrontendRedirectUrl(configuration, "login", ("externalError", "External login failed."));
                context.Response.Redirect(redirectUrl);
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    }

    internal static string BuildFrontendRedirectUrl(IConfiguration configuration, string path, params (string Key, string Value)[] query)
    {
        var frontendUrl = (configuration["Authentication:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var url = $"{frontendUrl}/{path.TrimStart('/')}";
        if (query.Length == 0)
            return url;

        var queryString = string.Join("&", query.Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));
        return $"{url}?{queryString}";
    }

    private static string ReplaceQueryValue(string url, string key, string value)
    {
        var uriBuilder = new UriBuilder(url);
        var query = uriBuilder.Query.TrimStart('?');
        var parts = string.IsNullOrWhiteSpace(query)
            ? new List<string>()
            : query.Split('&', StringSplitOptions.RemoveEmptyEntries).ToList();

        var replacement = $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        var replaced = false;
        for (var i = 0; i < parts.Count; i++)
        {
            var separatorIndex = parts[i].IndexOf('=', StringComparison.Ordinal);
            var currentKey = separatorIndex >= 0 ? parts[i][..separatorIndex] : parts[i];
            if (!string.Equals(Uri.UnescapeDataString(currentKey), key, StringComparison.Ordinal))
                continue;

            parts[i] = replacement;
            replaced = true;
            break;
        }

        if (!replaced)
            parts.Add(replacement);

        uriBuilder.Query = string.Join("&", parts);
        return uriBuilder.Uri.AbsoluteUri;
    }

    internal static void ValidateRequiredConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        var requiredKeys = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "Jwt:Key",
            "Jwt:Issuer",
            "Jwt:Audience",
            "Smtp:Host",
            "Smtp:Username",
            "Smtp:Password",
            "Authentication:BackendUrl",
            "Authentication:FrontendUrl",
            "Authentication:Google:ClientId",
            "Authentication:Google:ClientSecret",
            "Authentication:Facebook:AppId",
            "Authentication:Facebook:AppSecret",
            "SePay:ApiKey",
            "OpenAi:ApiKey",
            "R2Avatar:AccountId",
            "R2Avatar:BucketName",
            "R2Avatar:AccessKeyId",
            "R2Avatar:SecretAccessKey",
            "R2Avatar:PublicBaseUrl",
            "R2Share:AccountId",
            "R2Share:BucketName",
            "R2Share:AccessKeyId",
            "R2Share:SecretAccessKey",
            "R2Share:PublicBaseUrl"
        };

        var missingKeys = requiredKeys
            .Where(key =>
            {
                var value = configuration[key];
                return IsPlaceholder(value);
            })
            .ToArray();

        if (missingKeys.Length > 0)
            throw new InvalidOperationException(
                $"Missing or placeholder production configuration: {string.Join(", ", missingKeys)}");

        if ((configuration["Jwt:Key"]?.Length ?? 0) < 32)
            throw new InvalidOperationException("Production JWT signing key must be at least 32 characters.");
    }

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("_SECRET", StringComparison.OrdinalIgnoreCase);
}
