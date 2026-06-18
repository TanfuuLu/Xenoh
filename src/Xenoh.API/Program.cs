using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Scalar.AspNetCore;
using Xenoh.API.Auth;
using static Xenoh.API.Auth.ExternalAuthHelpers;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure;
using Xenoh.Infrastructure.BackgroundServices;
using Xenoh.Infrastructure.Middleware;
using Xenoh.Infrastructure.Persistence.Seeders;
using Xenoh.API.Security;

var builder = WebApplication.CreateBuilder(args);

ValidateRequiredConfiguration(builder.Configuration, builder.Environment);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    ForwardedHeadersSetup.ConfigureTrustedForwardedHeaders(options, builder.Configuration);
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediator(static options =>
    options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
})
.AddCookie("External", options =>
{
    options.Cookie.Name = "xenoh.external";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.SignInScheme = "External";
    options.CallbackPath = "/api/auth/external/google/callback";
    options.SaveTokens = false;
    options.ClaimActions.MapJsonKey("picture", "picture");
    options.Events = CreateExternalAuthEvents("Google", builder.Configuration);
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
    options.SignInScheme = "External";
    options.CallbackPath = "/api/auth/external/facebook/callback";
    options.SaveTokens = false;
    options.Fields.Add("email");
    options.Fields.Add("first_name");
    options.Fields.Add("last_name");
    options.Events = CreateExternalAuthEvents("Facebook", builder.Configuration);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SubscriptionPolicies.RequirePro, policy =>
        policy.Requirements.Add(new ActiveSubscriptionRequirement(
            PlanTier.ProIndividual,
            PlanTier.ProCoach)));

    options.AddPolicy(SubscriptionPolicies.RequireProCoach, policy =>
        policy.Requirements.Add(new ActiveSubscriptionRequirement(PlanTier.ProCoach)));
});
builder.Services.AddScoped<IAuthorizationHandler, ActiveSubscriptionAuthorizationHandler>();
builder.Services.AddSignalR();

builder.Services.AddHostedService<ContractExpiryService>();

builder.Services.AddXenohRateLimiting(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var frontendUrl = builder.Configuration["Authentication:FrontendUrl"]?.TrimEnd('/');
        var developmentOrigins = builder.Environment.IsDevelopment()
            ? new[]
            {
                "http://localhost:5173", "http://localhost:5174", "http://localhost:5175",
                "https://localhost:5173", "https://localhost:5174", "https://localhost:5175",
                "http://127.0.0.1:5173", "http://127.0.0.1:5174", "http://127.0.0.1:5175",
                "https://127.0.0.1:5173", "https://127.0.0.1:5174", "https://127.0.0.1:5175", "http://127.0.0.1:8085"
            }
            : [];
        // Public production frontend origin — applies in all environments.
        var productionOrigins = new[] { "https://www.xenoh.online"};
        // Additional origins (e.g. public domain served over the Cloudflare tunnel).
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        var origins = new[] { frontendUrl }
            .Concat(developmentOrigins)
            .Concat(productionOrigins)
            .Concat(configuredOrigins.Select(origin => origin?.TrimEnd('/')))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition");
    });

    // Public share endpoints — allow any origin (no credentials needed, images are public)
    options.AddPolicy("PublicSharePolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddResponseCaching();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Xenoh API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Apply migrations and seed baseline data (roles, dev admin, templates, foods).
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider, app.Environment.IsDevelopment());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Xenoh API";
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseForwardedHeaders();
app.UseXenohSecurityHeaders();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/share"))
    {
        await next();
        return;
    }

    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Accept";
        return Task.CompletedTask;
    });

    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("FrontendPolicy");
app.UseResponseCaching();
app.UseAuthentication();
app.UseRateLimiter();
app.UseTokenBlacklistMiddleware();
app.UseAuthorization();

// Prometheus: collect HTTP request metrics and expose the scrape endpoint at /metrics.
// .NET runtime metrics (GC, threadpool, exceptions) are collected automatically.
app.UseHttpMetrics();
if (builder.Configuration.GetValue("Security:ExposeMetrics", app.Environment.IsDevelopment()))
    app.MapMetrics();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
