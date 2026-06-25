using Mediator;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed record MealPlanDayResponse(
    Guid? Id,
    Guid UserId,
    DateOnly Date,
    string? Notes,
    List<MealPlanMealResponse> Meals,
    MealPlanTotals PlannedTotals,
    MealPlanTotals CheckedTotals,
    int TotalItemCount,
    int CheckedItemCount
);

public sealed record MealPlanMealResponse(
    Guid Id,
    string Name,
    int SortOrder,
    List<MealPlanItemResponse> Items,
    MealPlanTotals PlannedTotals,
    MealPlanTotals CheckedTotals
);

public sealed record MealPlanItemResponse(
    Guid Id,
    Guid FoodItemId,
    string NameVi,
    string NameEn,
    int SortOrder,
    decimal Grams,
    string? ServingLabelVi,
    string? ServingLabelEn,
    decimal? ServingCount,
    int PlannedCalories,
    decimal PlannedProteinG,
    decimal PlannedCarbsG,
    decimal PlannedFatG,
    bool IsChecked,
    DateTime? CheckedAt,
    Guid? FoodLogId
);

public sealed record MealPlanTotals(
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG
);

public sealed record UpsertMealPlanForDateCommand(
    DateOnly Date,
    string? Notes,
    List<UpsertMealPlanMealRequest> Meals,
    Guid? UserId = null
) : IRequest<MealPlanDayResponse>;

public sealed record UpsertMealPlanMealRequest(
    Guid? Id,
    string Name,
    int SortOrder,
    List<UpsertMealPlanItemRequest> Items
);

public sealed record UpsertMealPlanItemRequest(
    Guid? Id,
    Guid FoodItemId,
    int SortOrder,
    decimal? Grams,
    string? ServingLabel,
    decimal? ServingCount
);

public sealed record GetMealPlanForDateQuery(DateOnly Date, Guid? UserId = null) : IRequest<MealPlanDayResponse>;

public sealed record CheckMealPlanItemCommand(Guid ItemId) : IRequest<MealPlanDayResponse>;

public sealed record UncheckMealPlanItemCommand(Guid ItemId) : IRequest<MealPlanDayResponse>;
