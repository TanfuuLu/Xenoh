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
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (tokenBlacklist.IsTokenRevoked(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked." });
                return;
            }
        }

        await _next(context);
    }
}

public static class TokenBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenBlacklistMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenBlacklistMiddleware>();
    }
}
