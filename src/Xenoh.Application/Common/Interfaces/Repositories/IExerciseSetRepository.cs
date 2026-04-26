using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseSetRepository
{
    /// <summary>
    /// Tracked: set with all nested navigation needed by MarkSetComplete
    /// (Exercise → Sets, Exercise → DailyWorkout → Exercises → Sets,
    ///  Exercise → DailyWorkout → WeeklyWorkout → Plan).
    /// </summary>
    Task<ExerciseSet?> FindForCompleteAsync(Guid setId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
