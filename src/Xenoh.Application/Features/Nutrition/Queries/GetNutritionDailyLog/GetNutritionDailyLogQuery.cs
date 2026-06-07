using Mediator;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionDailyLog;

public sealed record GetNutritionDailyLogQuery(DateOnly Date, Guid? UserId = null) : IRequest<NutritionDailyLogResponse?>;
