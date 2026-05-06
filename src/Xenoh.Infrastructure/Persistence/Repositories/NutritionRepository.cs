using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class NutritionRepository(ApplicationDbContext db) : INutritionRepository
{
    public Task<NutritionProfile?> GetProfileAsync(Guid userId, CancellationToken ct) =>
        db.NutritionProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<NutritionProfile?> GetProfileAsNoTrackingAsync(Guid userId, CancellationToken ct) =>
        db.NutritionProfiles
          .AsNoTracking()
          .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<NutritionDailyLog?> GetDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct) =>
        db.NutritionDailyLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date, ct);

    public Task<NutritionDailyLog?> GetDailyLogAsNoTrackingAsync(Guid userId, DateOnly date, CancellationToken ct) =>
        db.NutritionDailyLogs
          .AsNoTracking()
          .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date, ct);

    public Task<List<NutritionHistoryItemResponse>> GetHistoryAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct) =>
        db.NutritionDailyLogs
          .AsNoTracking()
          .Where(l => l.UserId == userId && l.Date >= from && l.Date <= to)
          .OrderBy(l => l.Date)
          .Select(l => new NutritionHistoryItemResponse(l.Date, l.Calories, l.ProteinG, l.CarbsG, l.FatG))
          .ToListAsync(ct);

    public async Task AddProfileAsync(NutritionProfile profile, CancellationToken ct) =>
        await db.NutritionProfiles.AddAsync(profile, ct);

    public async Task AddDailyLogAsync(NutritionDailyLog log, CancellationToken ct) =>
        await db.NutritionDailyLogs.AddAsync(log, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
