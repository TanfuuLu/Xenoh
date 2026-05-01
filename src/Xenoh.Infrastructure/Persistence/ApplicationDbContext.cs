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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
