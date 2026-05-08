using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> ApplicationUsers { get; }
    DbSet<Plan> Plans { get; }
    DbSet<WeeklyWorkout> WeeklyWorkouts { get; }
    DbSet<DailyWorkout> DailyWorkouts { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<ExerciseSet> ExerciseSets { get; }
    DbSet<CoachClientRelationship> CoachClientRelationships { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetCode> PasswordResetCodes { get; }
    DbSet<ExternalAuthTicket> ExternalAuthTickets { get; }
    DbSet<ExerciseTemplate> ExerciseTemplates { get; }
    DbSet<UserExercisePR> UserExercisePRs { get; }
    DbSet<UserExercisePRHistory> UserExercisePRHistories { get; }
    DbSet<WorkoutHistory> WorkoutHistories { get; }
    DbSet<BodyweightLog> BodyweightLogs { get; }
    DbSet<PlanComment> PlanComments { get; }
    DbSet<WeeklyWorkoutComment> WeeklyWorkoutComments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    DbSet<PaymentOrder> PaymentOrders { get; }
    DbSet<CoachRating> CoachRatings { get; }
    DbSet<UserReport> UserReports { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<IdentityRole<Guid>> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }
    DbSet<NutritionProfile> NutritionProfiles { get; }
    DbSet<NutritionDailyLog> NutritionDailyLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
