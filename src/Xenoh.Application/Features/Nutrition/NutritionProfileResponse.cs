using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition;

public sealed record NutritionProfileResponse(
    ActivityLevel ActivityLevel,
    NutritionGoal Goal,
    decimal? TargetWeightKg,
    int? CustomCalorieTarget,
    decimal? ProteinPerKg,
    decimal? FatPerKg);
