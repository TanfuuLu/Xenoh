using Mediator;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;

public sealed record GetFoodLogsForDateQuery(DateOnly Date, Guid? UserId = null) : IRequest<FoodLogsForDateResponse>;

public sealed record FoodLogItemResponse(
    Guid Id,
    Guid FoodItemId,
    string NameVi,
    string NameEn,
    decimal Grams,
    string? ServingLabelVi,
    string? ServingLabelEn,
    decimal? ServingCount,
    int ComputedCalories,
    decimal ComputedProteinG,
    decimal ComputedCarbsG,
    decimal ComputedFatG
);

public sealed record FoodLogsTotals(
    int TotalCalories,
    decimal TotalProteinG,
    decimal TotalCarbsG,
    decimal TotalFatG
);

public sealed record FoodLogsForDateResponse(
    DateOnly Date,
    List<FoodLogItemResponse> Items,
    FoodLogsTotals Totals
);
