namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanWeekRules
{
    public static void ValidateStartDate(DateOnly startDate)
    {
        if (startDate.DayOfWeek != DayOfWeek.Monday)
            throw new InvalidOperationException("Meal plan week start date must be a Monday.");

        if (startDate > DateOnly.MaxValue.AddDays(-6))
            throw new InvalidOperationException("Meal plan week exceeds the supported date range.");
    }

}
