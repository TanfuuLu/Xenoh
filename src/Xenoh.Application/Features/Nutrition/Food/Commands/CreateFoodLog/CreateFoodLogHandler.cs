using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.CreateFoodLog;

public sealed class CreateFoodLogHandler(
    IApplicationDbContext db,
    INutritionRepository nutritionRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<CreateFoodLogCommand, FoodLogItemResponse>
{
    public async ValueTask<FoodLogItemResponse> Handle(CreateFoodLogCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var food = await db.FoodItems
            .Include(f => f.Servings)
            .FirstOrDefaultAsync(f => f.Id == request.FoodItemId, cancellationToken)
            ?? throw new InvalidOperationException($"FoodItem {request.FoodItemId} not found.");

        decimal grams;
        string? servingLabelVi = null;
        string? servingLabelEn = null;
        decimal? servingCount = null;

        if (request.ServingLabel is not null && request.ServingCount is not null)
        {
            var serving = food.Servings.FirstOrDefault(s =>
                string.Equals(s.LabelVi, request.ServingLabel, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Serving '{request.ServingLabel}' not found for food item.");

            servingLabelVi = serving.LabelVi;
            servingLabelEn = serving.LabelEn;
            servingCount = request.ServingCount.Value;
            grams = serving.Grams * request.ServingCount.Value;
        }
        else if (request.Grams is not null and > 0)
        {
            grams = request.Grams.Value;
        }
        else
        {
            throw new InvalidOperationException("Either Grams or (ServingLabel + ServingCount) must be provided.");
        }

        var ratio = grams / 100m;
        var log = new FoodLog
        {
            UserId = userId,
            FoodItemId = food.Id,
            Date = request.Date,
            Grams = grams,
            ServingLabelVi = servingLabelVi,
            ServingLabelEn = servingLabelEn,
            ServingCount = servingCount,
            ComputedCalories = (int)Math.Round(food.CaloriesPer100g * ratio),
            ComputedProteinG = Math.Round(food.ProteinPer100g * ratio, 2),
            ComputedCarbsG = Math.Round(food.CarbsPer100g * ratio, 2),
            ComputedFatG = Math.Round(food.FatPer100g * ratio, 2)
        };

        await db.FoodLogs.AddAsync(log, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await RecomputeDailyLogAsync(userId, request.Date, cancellationToken);

        return new FoodLogItemResponse(
            log.Id,
            food.Id,
            food.NameVi,
            food.NameEn,
            log.Grams,
            log.ServingLabelVi,
            log.ServingLabelEn,
            log.ServingCount,
            log.ComputedCalories,
            log.ComputedProteinG,
            log.ComputedCarbsG,
            log.ComputedFatG
        );
    }

    private async Task RecomputeDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct)
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

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to edit this user's food logs.");
    }
}
