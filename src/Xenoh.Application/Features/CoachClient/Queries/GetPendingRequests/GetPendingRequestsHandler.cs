using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.CoachClient.Queries.GetPendingRequests;

public sealed class GetPendingRequestsHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetPendingRequestsQuery, List<CoachRelationshipResponse>>
{
    public async ValueTask<List<CoachRelationshipResponse>> Handle(
        GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        return await coachClientRepo.GetPendingByCoachAsync(coachId, cancellationToken);
    }
}
