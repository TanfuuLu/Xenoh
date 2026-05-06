namespace Xenoh.Application.Features.Nutrition;

public sealed record NutritionDailyLogResponse(
    DateOnly Date,
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    string? Notes);
