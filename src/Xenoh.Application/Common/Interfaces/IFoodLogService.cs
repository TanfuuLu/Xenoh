using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface IFoodLogService
{
    Task<FoodLog> BuildFoodLogAsync(
        Guid userId,
        DateOnly date,
        Guid foodItemId,
        decimal? grams,
        string? servingLabel,
        decimal? servingCount,
        CancellationToken ct = default);

    Task RecomputeDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct = default);
}
