namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanTemplateRules
{
    public const int MaximumRangeDays = 90;

    public static int Validate(ApplyMealPlanTemplateCommand request)
    {
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("Meal plan end date must be on or after the start date.");

        var dayCount = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (dayCount > MaximumRangeDays)
            throw new InvalidOperationException($"Meal plan range can contain at most {MaximumRangeDays} days.");

        MealPlanDayBuilder.Validate(request.Notes, request.Meals);
        return dayCount;
    }
}
