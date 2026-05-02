using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure;
using Xenoh.Infrastructure.Middleware;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Seeders;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "http://localhost:5174",
                  "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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

// Seed roles and apply schema
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] roles = [UserRole.Individual, UserRole.Coach, UserRole.Admin];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var seededTemplates = ExerciseTemplateSeeder.GetTemplates();
        var existingTemplateNames = await db.ExerciseTemplates
            .Select(t => t.Name.ToLower())
            .ToListAsync();
        var existingTemplateNameSet = existingTemplateNames.ToHashSet();
        var missingTemplates = seededTemplates
            .Where(t => !existingTemplateNameSet.Contains(t.Name.ToLower()))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            db.ExerciseTemplates.AddRange(missingTemplates);
            await db.SaveChangesAsync();
        }

        var seededByName = seededTemplates.ToDictionary(t => t.Name.ToLower());
        var templatesToSync = await db.ExerciseTemplates.ToListAsync();
        var syncedAny = false;
        var toDelete = new List<Xenoh.Domain.Entities.ExerciseTemplate>();
        foreach (var template in templatesToSync)
        {
            if (!seededByName.TryGetValue(template.Name.ToLower(), out var seededTemplate))
            {
                // Remove system templates that are no longer in the seed list
                if (template.OwnerId == null)
                    toDelete.Add(template);
                continue;
            }

            if (template.ExerciseKind == seededTemplate.ExerciseKind &&
                template.EstimatedMet == seededTemplate.EstimatedMet &&
                template.ImageUrl == seededTemplate.ImageUrl)
                continue;

            template.ExerciseKind = seededTemplate.ExerciseKind;
            template.EstimatedMet = seededTemplate.EstimatedMet;
            template.ImageUrl = seededTemplate.ImageUrl;
            syncedAny = true;
        }

        if (toDelete.Count > 0)
        {
            db.ExerciseTemplates.RemoveRange(toDelete);
            syncedAny = true;
        }

        if (syncedAny)
            await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during startup initialization.");
    }
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

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseTokenBlacklistMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

static OAuthEvents CreateExternalAuthEvents(string provider, IConfiguration configuration)
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

static string BuildFrontendRedirectUrl(IConfiguration configuration, string path, params (string Key, string Value)[] query)
{
    var frontendUrl = (configuration["Authentication:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
    var url = $"{frontendUrl}/{path.TrimStart('/')}";
    if (query.Length == 0)
        return url;

    var queryString = string.Join("&", query.Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));
    return $"{url}?{queryString}";
}
