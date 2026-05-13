using Mediator;
using Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.CreateFoodLog;

public sealed record CreateFoodLogCommand(
    DateOnly Date,
    Guid FoodItemId,
    decimal? Grams,
    string? ServingLabel,
    decimal? ServingCount,
    Guid? UserId = null
) : IRequest<FoodLogItemResponse>;
