using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.UnloveTrainingDayShare;

public sealed record UnloveTrainingDayShareCommand(Guid ShareId) : IRequest<TrainingDayShareResponse>;
