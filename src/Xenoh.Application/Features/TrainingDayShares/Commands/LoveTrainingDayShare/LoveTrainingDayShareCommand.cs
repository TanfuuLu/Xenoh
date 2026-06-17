using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.LoveTrainingDayShare;

public sealed record LoveTrainingDayShareCommand(Guid ShareId) : IRequest<TrainingDayShareResponse>;
