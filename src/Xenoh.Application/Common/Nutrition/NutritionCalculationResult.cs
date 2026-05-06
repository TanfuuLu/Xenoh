namespace Xenoh.Application.Common.Nutrition;

public sealed record NutritionCalculationResult(
    IReadOnlyList<string> MissingFields,
    decimal? BodyweightKg,
    int? Age,
    int? Bmr,
    int? Tdee,
    int? RecommendedCalories,
    int? CalorieTarget,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG);
