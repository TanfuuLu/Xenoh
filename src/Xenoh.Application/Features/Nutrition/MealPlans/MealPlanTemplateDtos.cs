using Mediator;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed record ApplyMealPlanTemplateCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes,
    List<UpsertMealPlanMealRequest> Meals,
    Guid? UserId = null) : IRequest<ApplyMealPlanTemplateResponse>;

public sealed record ApplyMealPlanTemplateResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int AffectedDayCount);
