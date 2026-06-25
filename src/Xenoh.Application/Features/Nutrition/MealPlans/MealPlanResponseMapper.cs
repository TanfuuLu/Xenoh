using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanResponseMapper
{
    public static MealPlanDayResponse Empty(Guid userId, DateOnly date) =>
        new(null, userId, date, null, [], Zero(), Zero(), 0, 0);

    public static MealPlanDayResponse ToResponse(MealPlanDay day)
    {
        var meals = day.Meals
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .Select(ToMealResponse)
            .ToList();

        var allItems = meals.SelectMany(m => m.Items).ToList();

        return new MealPlanDayResponse(
            day.Id,
            day.UserId,
            day.Date,
            day.Notes,
            meals,
            Sum(allItems),
            Sum(allItems.Where(i => i.IsChecked)),
            allItems.Count,
            allItems.Count(i => i.IsChecked));
    }

    private static MealPlanMealResponse ToMealResponse(MealPlanMeal meal)
    {
        var items = meal.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAt)
            .Select(i => new MealPlanItemResponse(
                i.Id,
                i.FoodItemId,
                i.FoodItem.NameVi,
                i.FoodItem.NameEn,
                i.SortOrder,
                i.Grams,
                i.ServingLabelVi,
                i.ServingLabelEn,
                i.ServingCount,
                i.PlannedCalories,
                i.PlannedProteinG,
                i.PlannedCarbsG,
                i.PlannedFatG,
                i.IsChecked,
                i.CheckedAt,
                i.FoodLogId))
            .ToList();

        return new MealPlanMealResponse(
            meal.Id,
            meal.Name,
            meal.SortOrder,
            items,
            Sum(items),
            Sum(items.Where(i => i.IsChecked)));
    }

    private static MealPlanTotals Sum(IEnumerable<MealPlanItemResponse> items) =>
        new(
            items.Sum(i => i.PlannedCalories),
            items.Sum(i => i.PlannedProteinG),
            items.Sum(i => i.PlannedCarbsG),
            items.Sum(i => i.PlannedFatG));

    private static MealPlanTotals Zero() => new(0, 0, 0, 0);
}
