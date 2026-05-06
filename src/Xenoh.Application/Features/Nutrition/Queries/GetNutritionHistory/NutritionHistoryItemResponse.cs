namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;

public sealed record NutritionHistoryItemResponse(
    DateOnly Date,
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG);
