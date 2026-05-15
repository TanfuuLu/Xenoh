using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Xenoh.API.Auth;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure;
using Xenoh.Infrastructure.BackgroundServices;
using Xenoh.Infrastructure.Middleware;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Seeders;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;

var builder = WebApplication.CreateBuilder(args);

ValidateRequiredConfiguration(builder.Configuration, builder.Environment);

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

builder.Services.AddRateLimiter(options =>
{
    // Auth endpoints: 10 requests per minute per IP
    options.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // AI endpoints: 5 requests per minute per authenticated user (falls back to IP)
    options.AddPolicy("ai", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? ctx.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var frontendUrl = builder.Configuration["Authentication:FrontendUrl"]?.TrimEnd('/');
        var origins = new[]
            {
                frontendUrl,
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "https://localhost:5173",
                "https://localhost:5174",
                "https://localhost:5175"
            }
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
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

        if (app.Environment.IsDevelopment())
        {
        // Seed admin superuser account
        const string adminEmail = "admin@xenoh.app";
        const string adminPassword = "Admin@Xenoh123!";
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Email = adminEmail,
                UserName = adminEmail,
                FirstName = "Admin",
                LastName = "Xenoh",
            };
            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, [UserRole.Admin, UserRole.Individual, UserRole.Coach]);
            }
        }
        if (adminUser is not null)
        {
            var existingSub = await db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == adminUser.Id);
            if (existingSub is null)
            {
                db.UserSubscriptions.Add(new Xenoh.Domain.Entities.UserSubscription
                {
                    UserId = adminUser.Id,
                    Tier = PlanTier.ProCoach,
                    ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                });
                await db.SaveChangesAsync();
            }
            else if (existingSub.Tier != PlanTier.ProCoach)
            {
                existingSub.Tier = PlanTier.ProCoach;
                existingSub.ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                await db.SaveChangesAsync();
            }
        }

        // Seed coach demo account
        const string coachEmail = "coach@xenoh.app";
        const string coachPassword = "Coach@Xenoh123!";
        var coachUser = await userManager.FindByEmailAsync(coachEmail);
        if (coachUser is null)
        {
            coachUser = new ApplicationUser
            {
                Email = coachEmail,
                UserName = coachEmail,
                FirstName = "Demo",
                LastName = "Coach",
            };
            var coachResult = await userManager.CreateAsync(coachUser, coachPassword);
            if (coachResult.Succeeded)
                await userManager.AddToRolesAsync(coachUser, [UserRole.Coach, UserRole.Individual]);
        }
        if (coachUser is not null)
        {
            var coachSub = await db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == coachUser.Id);
            if (coachSub is null)
            {
                db.UserSubscriptions.Add(new Xenoh.Domain.Entities.UserSubscription
                {
                    UserId = coachUser.Id,
                    Tier = PlanTier.ProCoach,
                    ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                });
                await db.SaveChangesAsync();
            }
            else if (coachSub.Tier != PlanTier.ProCoach)
            {
                coachSub.Tier = PlanTier.ProCoach;
                coachSub.ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                await db.SaveChangesAsync();
            }
        }

        await DemoDataSeeder.SeedAsync(db, userManager);
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

        // Seed FoodItems
        var seededFoods = FoodItemSeeder.GetItems();
        var existingFoodNames = await db.FoodItems
            .Where(f => f.Source == Xenoh.Domain.Enums.FoodItemSource.Seed)
            .Select(f => f.NameEn.ToLower())
            .ToListAsync();
        var existingFoodNameSet = existingFoodNames.ToHashSet();
        foreach (var (food, servings) in seededFoods)
        {
            if (existingFoodNameSet.Contains(food.NameEn.ToLower()))
                continue;
            db.FoodItems.Add(food);
            await db.SaveChangesAsync();
            foreach (var (label, grams) in servings)
            {
                db.FoodServings.Add(new Xenoh.Domain.Entities.FoodServing
                {
                    FoodItemId = food.Id,
                    Label = label,
                    Grams = grams
                });
            }
        }
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
app.UseResponseCaching();
app.UseRateLimiter();
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

static void ValidateRequiredConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
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
