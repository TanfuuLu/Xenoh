using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Middleware;

public sealed class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklist tokenBlacklist)
    {
        var token = ExtractToken(context.Request);
        if (!string.IsNullOrEmpty(token) && await tokenBlacklist.IsTokenRevokedAsync(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked." });
            return;
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            return authHeader["Bearer ".Length..].Trim();

        // WebSocket/SSE transports cannot set headers, so JwtBearerEvents.OnMessageReceived
        // accepts ?access_token= on /hubs. Mirror that here or revoked tokens keep working
        // on the hubs until natural expiry.
        if (request.Path.StartsWithSegments("/hubs"))
        {
            var queryToken = request.Query["access_token"].ToString();
            if (!string.IsNullOrEmpty(queryToken))
                return queryToken;
        }

        return null;
    }
}

public static class TokenBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenBlacklistMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenBlacklistMiddleware>();
    }
}
