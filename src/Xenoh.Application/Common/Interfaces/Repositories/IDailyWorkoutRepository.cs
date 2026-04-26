using Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IDailyWorkoutRepository
{
    Task<bool> WeekAccessibleByUserAsync(Guid weeklyWorkoutId, Guid userId, CancellationToken ct = default);
    Task<List<DailyWorkoutResponse>> GetByWeekAsync(Guid weeklyWorkoutId, CancellationToken ct = default);

    /// <summary>Tracked: day with plan ownership info for edit permission checks.</summary>
    Task<DailyWorkout?> FindWithPlanAsync(Guid dailyWorkoutId, CancellationToken ct = default);

    /// <summary>Tracked: day with exercises + sets + plan (for Copy operation).</summary>
    Task<DailyWorkout?> FindWithExercisesAndPlanAsync(Guid dailyWorkoutId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
