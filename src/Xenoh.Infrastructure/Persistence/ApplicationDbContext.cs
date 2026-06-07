using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<WeeklyWorkout> WeeklyWorkouts => Set<WeeklyWorkout>();
    public DbSet<DailyWorkout> DailyWorkouts => Set<DailyWorkout>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseSet> ExerciseSets => Set<ExerciseSet>();
    public DbSet<CoachClientRelationship> CoachClientRelationships => Set<CoachClientRelationship>();
    public DbSet<CoachInviteCode> CoachInviteCodes => Set<CoachInviteCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<ExternalAuthTicket> ExternalAuthTickets => Set<ExternalAuthTicket>();
    public DbSet<ExerciseTemplate> ExerciseTemplates => Set<ExerciseTemplate>();
    public DbSet<UserExercisePR> UserExercisePRs => Set<UserExercisePR>();
    public DbSet<UserExercisePRHistory> UserExercisePRHistories => Set<UserExercisePRHistory>();
    public DbSet<WorkoutHistory> WorkoutHistories => Set<WorkoutHistory>();
    public DbSet<BodyweightLog> BodyweightLogs => Set<BodyweightLog>();
    public DbSet<PlanComment> PlanComments => Set<PlanComment>();
    public DbSet<WeeklyWorkoutComment> WeeklyWorkoutComments => Set<WeeklyWorkoutComment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<NutritionProfile> NutritionProfiles => Set<NutritionProfile>();
    public DbSet<NutritionDailyLog> NutritionDailyLogs => Set<NutritionDailyLog>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<FoodServing> FoodServings => Set<FoodServing>();
    public DbSet<FoodLog> FoodLogs => Set<FoodLog>();
    public DbSet<UserAnalysis> UserAnalyses => Set<UserAnalysis>();
    public DbSet<AiFeatureCache> AiFeatureCaches => Set<AiFeatureCache>();
    public DbSet<AiFeatureUsage> AiFeatureUsages => Set<AiFeatureUsage>();
    public DbSet<AiUsageQuota> AiUsageQuotas => Set<AiUsageQuota>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<Message> Messages => Set<Message>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
