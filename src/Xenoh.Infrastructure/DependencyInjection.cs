using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Identity;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

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
        services.AddScoped<IUserPrRepository, UserPrRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
        services.AddScoped<ICoachRatingRepository, CoachRatingRepository>();
        services.AddScoped<IUserReportRepository, UserReportRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<INutritionRepository, NutritionRepository>();
        services.AddScoped<IPowerliftingRepository, PowerliftingRepository>();
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 10;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITokenBlacklist, DatabaseTokenBlacklist>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICommentRealtimeService, CommentRealtimeService>();
        services.AddScoped<IUserAvatarStorageService, UserAvatarStorageService>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<IPrShareImageService, PrShareImageService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<ISePayWebhookVerifier, SePayWebhookVerifier>();
        services.AddSingleton<ISePayBankInfo, SePayBankInfo>();
        services.Configure<SePayOptions>(configuration.GetSection(SePayOptions.SectionName));

        // OpenAI-backed services
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddHttpClient<IUserAnalysisAi, OpenAiUserAnalysisAi>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddHttpClient<IFoodMacroAi, OpenAiFoodMacroAi>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        return services;
    }
}
