using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Xenoh.API.Security;

public static class RateLimitingSetup
{
    public static IServiceCollection AddXenohRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Global:PermitLimit", 300),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.Auth, context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Auth:PermitLimit", 10),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.RefreshToken, context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Auth:PermitLimit", 20),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.ExternalAuth, context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Auth:PermitLimit", 10),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.Ai, context =>
                CreateFixedWindowLimiter(
                    GetUserOrIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Ai:PermitLimit", 30),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.Webhook, context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    GetPermitLimit(configuration, "RateLimiting:Webhook:PermitLimit", 20),
                    TimeSpan.FromMinutes(1)));

            options.AddPolicy(RateLimitPolicyNames.PublicShare, context =>
                CreateFixedWindowLimiter(
                    GetIpPartitionKey(context),
                    60,
                    TimeSpan.FromMinutes(1)));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Too many requests. Please retry later." },
                    cancellationToken);
            };
        });

        return services;
    }

    public static string GetUserOrIpPartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) ? $"user:{userId}" : GetIpPartitionKey(context);
    }

    public static string GetIpPartitionKey(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "ip:unknown" : $"ip:{remoteIp}";
    }

    public static int GetPermitLimit(IConfiguration configuration, string key, int fallback)
    {
        var configured = configuration.GetValue<int?>(key);
        return configured is > 0 ? configured.Value : fallback;
    }

    private static RateLimitPartition<string> CreateFixedWindowLimiter(
        string partitionKey,
        int permitLimit,
        TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
