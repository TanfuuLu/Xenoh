using Mediator;

namespace Xenoh.Application.Features.Cycle.Queries.GetClientCycleOverview;

public sealed record GetClientCycleOverviewQuery(Guid ClientId)
    : IRequest<ClientCycleOverviewResponse>;
