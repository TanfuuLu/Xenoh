using Mediator;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;

public sealed record GetNutritionHistoryQuery(DateOnly From, DateOnly To, Guid? UserId = null)
    : IRequest<List<NutritionHistoryItemResponse>>;
