using Mediator;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleLogs;

public sealed record GetCycleLogsQuery(DateOnly From, DateOnly To)
    : IRequest<IReadOnlyList<CycleDailyLogResponse>>;
