namespace Xenoh.API.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("X-Frame-Options", "DENY");

        if (!environment.IsDevelopment())
        {
            headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            if (context.Request.Path.StartsWithSegments("/api"))
                headers.TryAdd("Cache-Control", "no-store");
        }

        await next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseXenohSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
