using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IWorkoutHistoryRepository
{
    Task<List<DateOnly>> GetSortedDatesDescAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<Dictionary<Guid, DateOnly?>> GetLastDatesForUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task AddAsync(WorkoutHistory history, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
