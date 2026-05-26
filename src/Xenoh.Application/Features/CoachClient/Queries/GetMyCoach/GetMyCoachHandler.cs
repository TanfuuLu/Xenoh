using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;

public sealed class GetMyCoachHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyCoachQuery, CoachRelationshipResponse?>
{
    public async ValueTask<CoachRelationshipResponse?> Handle(
        GetMyCoachQuery request, CancellationToken cancellationToken)
    {
        var clientId = currentUser.UserId;
        return await coachClientRepo.GetByClientWithDetailsAsync(clientId, cancellationToken);
    }
}
