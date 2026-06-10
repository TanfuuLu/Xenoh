using Mediator;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleDayMarkers;

public sealed record GetCycleDayMarkersQuery(DateOnly From, DateOnly To)
    : IRequest<CycleDayMarkersResponse>;
