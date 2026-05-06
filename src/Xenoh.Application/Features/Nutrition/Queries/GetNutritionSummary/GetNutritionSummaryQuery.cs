using Mediator;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;

public sealed record GetNutritionSummaryQuery(Guid? UserId = null) : IRequest<NutritionSummaryResponse>;
