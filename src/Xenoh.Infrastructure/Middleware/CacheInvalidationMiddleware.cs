using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Middleware;

/// <summary>
/// Centralizes post-write cache invalidation for the existing command-heavy API.
/// It deliberately invalidates a small number of version tags rather than trying
/// to infer every aggregate relationship from HTTP routes.
/// </summary>
public sealed class CacheInvalidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICacheInvalidator invalidator)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        await next(context);
        if (context.Response.StatusCode is < 200 or >= 400)
            return;

        var tags = new List<string> { CacheTags.CoachDashboards };
        var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
            tags.Add(CacheTags.User(userId));

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/nutrition/foods", StringComparison.OrdinalIgnoreCase))
            tags.Add(CacheTags.Foods);
        if (path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/exercises", StringComparison.OrdinalIgnoreCase))
            tags.Add(CacheTags.Leaderboards);
        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase))
            tags.Add(CacheTags.Admin);

        await invalidator.InvalidateAsync(tags, context.RequestAborted);
    }
}

public static class CacheInvalidationMiddlewareExtensions
{
    public static IApplicationBuilder UseCacheInvalidation(this IApplicationBuilder app) =>
        app.UseMiddleware<CacheInvalidationMiddleware>();
}
