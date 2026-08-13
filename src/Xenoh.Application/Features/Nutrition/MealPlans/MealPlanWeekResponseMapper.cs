namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanWeekResponseMapper
{
    public static MealPlanWeekResponse ToResponse(DateOnly startDate, List<MealPlanDayResponse> days) =>
        new(
            startDate,
            startDate.AddDays(6),
            days,
            Sum(days.Select(d => d.PlannedTotals)),
            Sum(days.Select(d => d.CheckedTotals)),
            days.Sum(d => d.TotalItemCount),
            days.Sum(d => d.CheckedItemCount));

    private static MealPlanTotals Sum(IEnumerable<MealPlanTotals> totals) =>
        new(
            totals.Sum(t => t.Calories),
            totals.Sum(t => t.ProteinG),
            totals.Sum(t => t.CarbsG),
            totals.Sum(t => t.FatG));
}
