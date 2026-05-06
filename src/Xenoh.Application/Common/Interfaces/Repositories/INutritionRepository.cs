using Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface INutritionRepository
{
    Task<NutritionProfile?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<NutritionProfile?> GetProfileAsNoTrackingAsync(Guid userId, CancellationToken ct = default);
    Task<NutritionDailyLog?> GetDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<NutritionDailyLog?> GetDailyLogAsNoTrackingAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<List<NutritionHistoryItemResponse>> GetHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task AddProfileAsync(NutritionProfile profile, CancellationToken ct = default);
    Task AddDailyLogAsync(NutritionDailyLog log, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
