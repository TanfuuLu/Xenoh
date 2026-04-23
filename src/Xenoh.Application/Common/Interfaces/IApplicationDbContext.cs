using Microsoft.EntityFrameworkCore;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<WeeklyWorkout> WeeklyWorkouts { get; }
    DbSet<DailyWorkout> DailyWorkouts { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<ExerciseSet> ExerciseSets { get; }
    DbSet<CoachClientRelationship> CoachClientRelationships { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ExerciseTemplate> ExerciseTemplates { get; }
    DbSet<UserExercisePR> UserExercisePRs { get; }
    DbSet<WorkoutHistory> WorkoutHistories { get; }
    DbSet<BodyweightLog> BodyweightLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
