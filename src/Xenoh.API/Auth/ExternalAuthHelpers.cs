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
            "Authentication:FrontendUrl",
            "Authentication:Google:ClientId",
            "Authentication:Google:ClientSecret",
            "Authentication:Facebook:AppId",
            "Authentication:Facebook:AppSecret",
            "SePay:ApiKey",
            "OpenAi:ApiKey"
        };

        var missingKeys = requiredKeys
            .Where(key =>
            {
                var value = configuration[key];
                return string.IsNullOrWhiteSpace(value) ||
                       value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
                       value.Contains("_SECRET", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        if (missingKeys.Length > 0)
            throw new InvalidOperationException(
                $"Missing or placeholder production configuration: {string.Join(", ", missingKeys)}");
    }
}
