using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IWeeklyWorkoutRepository
{
    Task<bool> PlanAccessibleByUserAsync(Guid planId, Guid userId, CancellationToken ct = default);
    Task<PagedResponse<WeeklyWorkoutResponse>> GetByPlanAsync(Guid planId, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>Tracked: week with plan + days for mutation.</summary>
    Task<WeeklyWorkout?> FindForMutationAsync(Guid weekId, CancellationToken ct = default);

    void RemoveRange(IEnumerable<WeeklyWorkout> weeks);
    void AddRange(IEnumerable<WeeklyWorkout> weeks);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
