using Mediator;

namespace Xenoh.Application.Features.Cycle.Commands.DeleteCycleDailyLog;

public sealed record DeleteCycleDailyLogCommand(DateOnly Date) : IRequest<Unit>;
