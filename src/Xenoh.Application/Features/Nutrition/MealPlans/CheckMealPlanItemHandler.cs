using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class CheckMealPlanItemHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
    ICurrentUserService currentUser
) : IRequestHandler<CheckMealPlanItemCommand, MealPlanDayResponse>
{
    public async ValueTask<MealPlanDayResponse> Handle(CheckMealPlanItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.MealPlanItems
            .Include(i => i.MealPlanMeal)
                .ThenInclude(m => m.MealPlanDay)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new InvalidOperationException("Meal plan item not found.");

        var day = item.MealPlanMeal.MealPlanDay;
        if (day.UserId != currentUser.UserId)
            throw new UnauthorizedAccessException("Only the user can check their own meal plan items.");

        if (!item.IsChecked)
        {
            var log = new FoodLog
            {
                UserId = day.UserId,
                FoodItemId = item.FoodItemId,
                Date = day.Date,
                Grams = item.Grams,
                ServingLabelVi = item.ServingLabelVi,
                ServingLabelEn = item.ServingLabelEn,
                ServingCount = item.ServingCount,
                ComputedCalories = item.PlannedCalories,
                ComputedProteinG = item.PlannedProteinG,
                ComputedCarbsG = item.PlannedCarbsG,
                ComputedFatG = item.PlannedFatG
            };

            await db.FoodLogs.AddAsync(log, cancellationToken);

            item.IsChecked = true;
            item.CheckedAt = DateTime.UtcNow;
            item.CheckedByUserId = currentUser.UserId;
            item.FoodLogId = log.Id;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            await foodLogService.RecomputeDailyLogAsync(day.UserId, day.Date, cancellationToken);
        }

        return await LoadResponseAsync(day.Id, cancellationToken);
    }

    private async Task<MealPlanDayResponse> LoadResponseAsync(Guid dayId, CancellationToken ct)
    {
        var day = await MealPlanQueryLoader.LoadAsNoTrackingByDayIdAsync(db, dayId, ct)
            ?? throw new InvalidOperationException("Meal plan could not be loaded.");
        return MealPlanResponseMapper.ToResponse(day);
    }
}
