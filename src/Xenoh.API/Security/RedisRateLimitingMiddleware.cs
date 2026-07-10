using Microsoft.AspNetCore.RateLimiting;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.API.Security;

public sealed class RedisRateLimitingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRedisRateLimiter limiter)
    {
        if (!limiter.IsEnabled)
        {
            await next(context);
            return;
        }

        var global = await limiter.AcquireAsync(
            "global", RateLimitingSetup.GetIpPartitionKey(context), context.RequestAborted);
        if (!await ContinueAsync(context, global))
            return;

        var policy = context.GetEndpoint()?
            .Metadata.GetMetadata<EnableRateLimitingAttribute>()?
            .PolicyName;
        if (!string.IsNullOrWhiteSpace(policy))
        {
            var partition = string.Equals(policy, RateLimitPolicyNames.Ai, StringComparison.Ordinal)
                ? RateLimitingSetup.GetUserOrIpPartitionKey(context)
                : RateLimitingSetup.GetIpPartitionKey(context);
            var lease = await limiter.AcquireAsync(policy, partition, context.RequestAborted);
            if (!await ContinueAsync(context, lease))
                return;
        }

        await next(context);
    }

    private static async Task<bool> ContinueAsync(HttpContext context, RedisRateLimitLease lease)
    {
        if (lease.IsAllowed)
            return true;

        context.Response.StatusCode = lease.IsAvailable
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status503ServiceUnavailable;
        if (lease.IsAvailable)
            context.Response.Headers.RetryAfter = Math.Ceiling(lease.RetryAfter.TotalSeconds).ToString("0");

        await context.Response.WriteAsJsonAsync(new
        {
            message = lease.IsAvailable
                ? "Too many requests. Please retry later."
                : "Rate limiting is temporarily unavailable. Please retry later."
        }, context.RequestAborted);
        return false;
    }
}

public static class RedisRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRedisRateLimiting(this IApplicationBuilder app) =>
        app.UseMiddleware<RedisRateLimitingMiddleware>();
}
