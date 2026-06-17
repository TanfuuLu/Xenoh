using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.DeleteTrainingDayShare;

public sealed record DeleteTrainingDayShareCommand(Guid ShareId) : IRequest;
