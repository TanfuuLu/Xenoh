using Mediator;
using Xenoh.Application.Features.Nutrition;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionDailyLog;

public sealed record GetNutritionDailyLogQuery(DateOnly Date, Guid? UserId = null) : IRequest<NutritionDailyLogResponse?>;
