using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Nutrition;

public sealed class FoodLogService(
    IApplicationDbContext db,
    INutritionRepository nutritionRepo
) : IFoodLogService
{
    public async Task<FoodLog> BuildFoodLogAsync(
        Guid userId,
        DateOnly date,
        Guid foodItemId,
        decimal? grams,
        string? servingLabel,
        decimal? servingCount,
        CancellationToken ct = default)
    {
        var trackedFood = db.FoodItems.Local.FirstOrDefault(f => f.Id == foodItemId);
        var trackedFoodEntry = db.ChangeTracker.Entries<FoodItem>()
            .FirstOrDefault(entry => entry.Entity.Id == foodItemId);
        var food = trackedFood is not null
                   && trackedFoodEntry is not null
                   && trackedFoodEntry.Collection(f => f.Servings).IsLoaded
            ? trackedFood
            : await db.FoodItems
                .Include(f => f.Servings)
                .FirstOrDefaultAsync(f => f.Id == foodItemId, ct)
            ?? throw new InvalidOperationException($"FoodItem {foodItemId} not found.");

        decimal computedGrams;
        string? servingLabelVi = null;
        string? servingLabelEn = null;
        decimal? computedServingCount = null;

        var hasServingLabel = !string.IsNullOrWhiteSpace(servingLabel);
        var hasServingCount = servingCount.HasValue;

        if (hasServingLabel || hasServingCount)
        {
            if (!hasServingLabel || !hasServingCount || servingCount <= 0)
                throw new InvalidOperationException("ServingLabel and a positive ServingCount must be provided together.");

            var serving = food.Servings.FirstOrDefault(s =>
                string.Equals(s.LabelVi, servingLabel, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Serving '{servingLabel}' not found for food item.");

            servingLabelVi = serving.LabelVi;
            servingLabelEn = serving.LabelEn;
            computedServingCount = servingCount!.Value;
            computedGrams = serving.Grams * servingCount.Value;
        }
        else if (grams is not null and > 0)
        {
            computedGrams = grams.Value;
        }
        else
        {
            throw new InvalidOperationException("Either Grams or (ServingLabel + ServingCount) must be provided.");
        }

        var ratio = computedGrams / 100m;

        return new FoodLog
        {
            UserId = userId,
            FoodItemId = food.Id,
            FoodItem = food,
            Date = date,
            Grams = computedGrams,
            ServingLabelVi = servingLabelVi,
            ServingLabelEn = servingLabelEn,
            ServingCount = computedServingCount,
            ComputedCalories = (int)Math.Round(food.CaloriesPer100g * ratio),
            ComputedProteinG = Math.Round(food.ProteinPer100g * ratio, 2),
            ComputedCarbsG = Math.Round(food.CarbsPer100g * ratio, 2),
            ComputedFatG = Math.Round(food.FatPer100g * ratio, 2)
        };
    }

    public async Task RecomputeDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var totals = await db.FoodLogs
            .Where(l => l.UserId == userId && l.Date == date)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Calories = g.Sum(l => l.ComputedCalories),
                ProteinG = g.Sum(l => l.ComputedProteinG),
                CarbsG = g.Sum(l => l.ComputedCarbsG),
                FatG = g.Sum(l => l.ComputedFatG)
            })
            .FirstOrDefaultAsync(ct);

        var dailyLog = await nutritionRepo.GetDailyLogAsync(userId, date, ct);

        if (totals is null)
        {
            if (dailyLog is not null)
            {
                dailyLog.Calories = 0;
                dailyLog.ProteinG = 0;
                dailyLog.CarbsG = 0;
                dailyLog.FatG = 0;
                dailyLog.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            if (dailyLog is null)
            {
                dailyLog = new NutritionDailyLog { UserId = userId, Date = date };
                await nutritionRepo.AddDailyLogAsync(dailyLog, ct);
            }

            dailyLog.Calories = totals.Calories;
            dailyLog.ProteinG = totals.ProteinG;
            dailyLog.CarbsG = totals.CarbsG;
            dailyLog.FatG = totals.FatG;
            dailyLog.UpdatedAt = DateTime.UtcNow;
        }

        await nutritionRepo.SaveChangesAsync(ct);
    }
}
