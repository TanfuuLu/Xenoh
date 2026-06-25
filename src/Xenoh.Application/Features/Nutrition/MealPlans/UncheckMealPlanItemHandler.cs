using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class UncheckMealPlanItemHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
    ICurrentUserService currentUser
) : IRequestHandler<UncheckMealPlanItemCommand, MealPlanDayResponse>
{
    public async ValueTask<MealPlanDayResponse> Handle(UncheckMealPlanItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.MealPlanItems
            .Include(i => i.MealPlanMeal)
                .ThenInclude(m => m.MealPlanDay)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new InvalidOperationException("Meal plan item not found.");

        var day = item.MealPlanMeal.MealPlanDay;
        if (day.UserId != currentUser.UserId)
            throw new UnauthorizedAccessException("Only the user can uncheck their own meal plan items.");

        if (item.IsChecked)
        {
            if (item.FoodLogId is not null)
            {
                var log = await db.FoodLogs.FirstOrDefaultAsync(l => l.Id == item.FoodLogId.Value, cancellationToken);
                if (log is not null)
                    db.FoodLogs.Remove(log);
            }

            item.IsChecked = false;
            item.CheckedAt = null;
            item.CheckedByUserId = null;
            item.FoodLogId = null;
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
