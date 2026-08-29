using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Nutrition;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Identity;
using Xenoh.Infrastructure.Caching;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<RedisApplicationCache>();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, RedisRevocationWarmupService>();
        services.AddSingleton<Xenoh.Application.Common.Interfaces.IApplicationCache>(provider =>
            provider.GetRequiredService<RedisApplicationCache>());
        services.AddSingleton<Xenoh.Application.Common.Interfaces.ICacheInvalidator, RedisCacheInvalidator>();
        services.AddSingleton<Xenoh.Application.Common.Interfaces.IDistributedLock, RedisDistributedLock>();
        services.AddSingleton<Xenoh.Application.Common.Interfaces.IRedisRateLimiter, RedisRateLimiter>();

        // Allow selecting which connection string to use (e.g. the IPv4 Supabase
        // pooler when running inside Docker). Defaults to DefaultConnection.
        var connectionName = configuration["ConnectionStringName"] ?? "DefaultConnection";
        var connectionString = BuildConnectionString(configuration, connectionName);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICoachClientRepository, CoachClientRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IWeeklyWorkoutRepository, WeeklyWorkoutRepository>();
        services.AddScoped<IDailyWorkoutRepository, DailyWorkoutRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IExerciseSetRepository, ExerciseSetRepository>();
        services.AddScoped<IExerciseTemplateRepository, ExerciseTemplateRepository>();
        services.AddScoped<IBodyweightRepository, BodyweightRepository>();
        services.AddScoped<IWorkoutHistoryRepository, WorkoutHistoryRepository>();
        services.AddScoped<ITrainingActivityRepository, TrainingActivityRepository>();
        services.AddScoped<IUserPrRepository, UserPrRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
        services.AddScoped<IUserReportRepository, UserReportRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<INutritionRepository, NutritionRepository>();
        services.AddScoped<IPowerliftingRepository, PowerliftingRepository>();
        services.AddScoped<ISupplementRepository, SupplementRepository>();
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            // Without these, CheckPasswordSignInAsync's lockout path never fires and password
            // guessing is unbounded. See LoginHandler.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITokenBlacklist, RedisTokenBlacklist>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAccountDeletionService, AccountDeletionService>();
        services.AddScoped<IFoodLogService, FoodLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICommentRealtimeService, CommentRealtimeService>();
        services.AddScoped<IChatRealtimeService, ChatRealtimeService>();
        services.Configure<R2AvatarOptions>(configuration.GetSection(R2AvatarOptions.SectionName));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<R2AvatarOptions>>().Value;
            return new AmazonS3Client(
                new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = $"https://{opts.AccountId}.r2.cloudflarestorage.com",
                    AuthenticationRegion = "auto"
                });
        });
        services.AddScoped<IUserAvatarStorageService, UserAvatarStorageService>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Avatar fetching for share cards renders a user-influenced URL server-side (OAuth
        // providers supply the initial AvatarUrl verbatim). Redirects are disabled so a 302
        // cannot bounce the request past the host allowlist in PrShareImageService, and both
        // the timeout and buffer cap bound what a hostile endpoint can cost us.
        services.AddHttpClient(PrShareImageService.AvatarHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 5 * 1024 * 1024;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.AddScoped<IPrShareImageService, PrShareImageService>();
        services.Configure<R2ShareOptions>(configuration.GetSection(R2ShareOptions.SectionName));
        services.AddScoped<IPrShareImageStorage, PrShareImageStorageService>();
        services.Configure<R2DocumentOptions>(configuration.GetSection(R2DocumentOptions.SectionName));
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddScoped<ICompetitionDocumentStorageService, CompetitionDocumentStorageService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISubscriptionActivationService, SubscriptionActivationService>();
        services.AddScoped<IAiQuotaService, AiQuotaService>();
        services.AddSingleton<ISePayWebhookVerifier, SePayWebhookVerifier>();
        services.AddSingleton<ISePayBankInfo, SePayBankInfo>();
        services.Configure<SePayOptions>(configuration.GetSection(SePayOptions.SectionName));
        services.AddHttpClient<IPaymentPreflightService, PaymentPreflightService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SePayOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.HealthTimeoutSeconds + 5);
        });

        // OpenAI-backed services
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddHttpClient<OpenAiUserAnalysisAi>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddScoped<IUserAnalysisAi, QuotaEnforcedUserAnalysisAi>();

        services.AddHttpClient<OpenAiFoodMacroAi>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddScoped<IFoodMacroAi, QuotaEnforcedFoodMacroAi>();

        services.AddHttpClient<OpenAiCycleInsightAi>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddScoped<ICycleInsightAi, QuotaEnforcedCycleInsightAi>();

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration, string connectionName)
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (configuration.GetValue<int?>("Database:MaxPoolSize") is { } maxPoolSize && maxPoolSize > 0)
        {
            builder.MaxPoolSize = maxPoolSize;
        }
        else if (!HasConnectionStringKey(connectionString, "Maximum Pool Size") &&
                 !HasConnectionStringKey(connectionString, "MaxPoolSize"))
        {
            builder.MaxPoolSize = 10;
        }

        if (configuration.GetValue<int?>("Database:TimeoutSeconds") is { } timeoutSeconds && timeoutSeconds > 0)
            builder.Timeout = timeoutSeconds;

        if (configuration.GetValue<int?>("Database:CommandTimeoutSeconds") is { } commandTimeoutSeconds && commandTimeoutSeconds > 0)
            builder.CommandTimeout = commandTimeoutSeconds;

        if (configuration.GetValue<int?>("Database:KeepAliveSeconds") is { } keepAliveSeconds && keepAliveSeconds > 0)
            builder.KeepAlive = keepAliveSeconds;

        return builder.ConnectionString;
    }

    private static bool HasConnectionStringKey(string connectionString, string key) =>
        connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => part.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase));
}
