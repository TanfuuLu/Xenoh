using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class BodyweightRepository(ApplicationDbContext db) : IBodyweightRepository
{
    public Task<List<BodyweightLogResponse>> GetHistoryAsync(Guid userId, DateOnly from, CancellationToken ct) =>
        db.BodyweightLogs
          .AsNoTracking()
          .Where(b => b.UserId == userId && b.Date >= from)
          .OrderByDescending(b => b.Date)
          .Select(b => new BodyweightLogResponse(b.Id, b.Weight, b.Date))
          .ToListAsync(ct);

    public Task<decimal?> GetLatestWeightAsync(Guid userId, CancellationToken ct) =>
        db.BodyweightLogs
          .AsNoTracking()
          .Where(b => b.UserId == userId)
          .OrderByDescending(b => b.Date)
          .Select(b => (decimal?)b.Weight)
          .FirstOrDefaultAsync(ct);

    public async Task<Dictionary<Guid, decimal?>> GetLatestWeightsForUsersAsync(
        IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        return await db.BodyweightLogs
          .AsNoTracking()
          .Where(b => ids.Contains(b.UserId))
          .GroupBy(b => b.UserId)
          .Select(g => new { UserId = g.Key, Weight = g.OrderByDescending(b => b.Date).First().Weight })
          .ToDictionaryAsync(x => x.UserId, x => (decimal?)x.Weight, ct);
    }

    public Task<BodyweightLog?> FindTodayAsync(Guid userId, DateOnly today, CancellationToken ct) =>
        db.BodyweightLogs
          .FirstOrDefaultAsync(b => b.UserId == userId && b.Date == today, ct);

    public Task<BodyweightLog?> FindByIdAndOwnerAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.BodyweightLogs
          .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, ct);

    public async Task AddAsync(BodyweightLog log, CancellationToken ct) =>
        await db.BodyweightLogs.AddAsync(log, ct);

    public void Remove(BodyweightLog log) => db.BodyweightLogs.Remove(log);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
