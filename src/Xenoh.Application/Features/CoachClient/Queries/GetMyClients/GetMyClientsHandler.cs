using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyClients;

public sealed class GetMyClientsHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyClientsQuery, List<ClientResponse>>
{
    public async ValueTask<List<ClientResponse>> Handle(
        GetMyClientsQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        return await coachClientRepo.GetAllByCoachAsync(coachId, cancellationToken);
    }
}
