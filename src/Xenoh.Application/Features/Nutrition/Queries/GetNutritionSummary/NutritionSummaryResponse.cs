using Xenoh.Application.Common.Nutrition;
using Xenoh.Application.Features.Nutrition;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;

public sealed record NutritionSummaryResponse(
    Guid UserId,
    NutritionProfileResponse Profile,
    NutritionCalculationResult Calculation,
    NutritionDailyLogResponse? TodayLog,
    bool CanUseAdvancedAnalysis);
