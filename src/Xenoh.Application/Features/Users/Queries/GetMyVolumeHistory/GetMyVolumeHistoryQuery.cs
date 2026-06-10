using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetMyVolumeHistory;

public sealed record GetMyVolumeHistoryQuery(int Months)
    : IRequest<IReadOnlyList<VolumeHistoryPoint>>;
