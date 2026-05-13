using Mediator;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.DeleteFoodLog;

public sealed record DeleteFoodLogCommand(Guid FoodLogId, Guid? UserId = null) : IRequest;
