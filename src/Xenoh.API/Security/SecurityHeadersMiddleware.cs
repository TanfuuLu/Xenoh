namespace Xenoh.API.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    // Content-Security-Policy tuned to Xenoh's dependencies: AntD v5 + Tailwind inject
    // inline <style> (style-src 'unsafe-inline'); SignalR uses wss; images are served
    // from the R2 avatar/asset hosts. The SPA HTML is served by nginx, which carries its
    // own copy of this policy — this one covers direct API responses and the Scalar docs.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://avatar.xenoh.online https://assets.xenoh.online; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://api.xenoh.online wss://api.xenoh.online; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Content-Security-Policy", ContentSecurityPolicy);

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
