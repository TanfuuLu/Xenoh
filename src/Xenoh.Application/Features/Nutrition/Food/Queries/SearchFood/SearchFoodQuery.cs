using Mediator;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;

public sealed record SearchFoodQuery(string Query, string Language = "vi") : IRequest<List<FoodItemResponse>>;

public sealed record FoodServingResponse(Guid Id, string Label, decimal Grams);

public sealed record FoodItemResponse(
    Guid Id,
    string NameVi,
    string NameEn,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    List<FoodServingResponse> Servings
);
