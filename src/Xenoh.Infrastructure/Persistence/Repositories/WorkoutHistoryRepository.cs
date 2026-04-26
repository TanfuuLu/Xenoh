using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class WorkoutHistoryRepository(ApplicationDbContext db) : IWorkoutHistoryRepository
{
    public Task<List<DateOnly>> GetSortedDatesDescAsync(Guid userId, CancellationToken ct) =>
        db.WorkoutHistories
          .AsNoTracking()
          .Where(h => h.UserId == userId)
          .OrderByDescending(h => h.Date)
          .Select(h => h.Date)
          .ToListAsync(ct);

    public Task<bool> ExistsForDateAsync(Guid userId, DateOnly date, CancellationToken ct) =>
        db.WorkoutHistories
          .AnyAsync(h => h.UserId == userId && h.Date == date, ct);

    public async Task<Dictionary<Guid, DateOnly?>> GetLastDatesForUsersAsync(
        IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        return await db.WorkoutHistories
          .AsNoTracking()
          .Where(h => ids.Contains(h.UserId))
          .GroupBy(h => h.UserId)
          .Select(g => new { UserId = g.Key, LastDate = g.Max(h => h.Date) })
          .ToDictionaryAsync(x => x.UserId, x => (DateOnly?)x.LastDate, ct);
    }

    public async Task AddAsync(WorkoutHistory history, CancellationToken ct) =>
        await db.WorkoutHistories.AddAsync(history, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
