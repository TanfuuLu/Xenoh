using Mediator;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed record MealPlanWeekResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    List<MealPlanDayResponse> Days,
    MealPlanTotals PlannedTotals,
    MealPlanTotals CheckedTotals,
    int TotalItemCount,
    int CheckedItemCount);

public sealed record GetMealPlanWeekQuery(
    DateOnly StartDate,
    Guid? UserId = null) : IRequest<MealPlanWeekResponse>;
