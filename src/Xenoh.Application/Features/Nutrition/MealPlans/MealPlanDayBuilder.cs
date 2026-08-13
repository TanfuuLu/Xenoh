using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanDayBuilder
{
    public static void Validate(string? notes, IReadOnlyCollection<UpsertMealPlanMealRequest> meals)
    {
        if (notes?.Length > 500)
            throw new InvalidOperationException("Meal plan notes must be 500 characters or less.");

        if (meals is null)
            throw new InvalidOperationException("Meals are required for each meal plan day.");

        if (meals.Count > 12)
            throw new InvalidOperationException("Meal plan can contain at most 12 meals per day.");

        if (meals.Any(static meal => meal is null))
            throw new InvalidOperationException("Meals cannot contain null entries.");

        foreach (var meal in meals)
        {
            if (string.IsNullOrWhiteSpace(meal.Name))
                throw new InvalidOperationException("Meal name is required.");

            if (meal.Name.Length > 100)
                throw new InvalidOperationException("Meal name must be 100 characters or less.");

            if (meal.Items is null)
                throw new InvalidOperationException("Meal items are required.");

            if (meal.Items.Count > 30)
                throw new InvalidOperationException("A meal can contain at most 30 items.");

            if (meal.Items.Any(static item => item is null))
                throw new InvalidOperationException("Meal items cannot contain null entries.");
        }
    }

    public static async Task<List<MealPlanMeal>> BuildMealsAsync(
        IFoodLogService foodLogService,
        Guid userId,
        DateOnly date,
        IReadOnlyCollection<UpsertMealPlanMealRequest> requests,
        CancellationToken ct)
    {
        var meals = new List<MealPlanMeal>(requests.Count);

        foreach (var mealRequest in requests.OrderBy(m => m.SortOrder))
        {
            var meal = new MealPlanMeal
            {
                Name = mealRequest.Name.Trim(),
                SortOrder = mealRequest.SortOrder
            };

            foreach (var itemRequest in mealRequest.Items.OrderBy(i => i.SortOrder))
            {
                var snapshot = await foodLogService.BuildFoodLogAsync(
                    userId,
                    date,
                    itemRequest.FoodItemId,
                    itemRequest.Grams,
                    itemRequest.ServingLabel,
                    itemRequest.ServingCount,
                    ct);

                meal.Items.Add(new MealPlanItem
                {
                    MealPlanMealId = meal.Id,
                    FoodItemId = snapshot.FoodItemId,
                    FoodItem = snapshot.FoodItem,
                    SortOrder = itemRequest.SortOrder,
                    Grams = snapshot.Grams,
                    ServingLabelVi = snapshot.ServingLabelVi,
                    ServingLabelEn = snapshot.ServingLabelEn,
                    ServingCount = snapshot.ServingCount,
                    PlannedCalories = snapshot.ComputedCalories,
                    PlannedProteinG = snapshot.ComputedProteinG,
                    PlannedCarbsG = snapshot.ComputedCarbsG,
                    PlannedFatG = snapshot.ComputedFatG
                });
            }

            meals.Add(meal);
        }

        return meals;
    }
}
