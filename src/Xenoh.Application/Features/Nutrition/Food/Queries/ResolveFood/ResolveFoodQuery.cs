using Mediator;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.ResolveFood;

public sealed record ResolveFoodQuery(string Name) : IRequest<FoodItemResponse>;
