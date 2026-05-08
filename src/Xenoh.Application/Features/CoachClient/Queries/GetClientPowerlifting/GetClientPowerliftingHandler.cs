using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.CoachClient.Queries.GetClientPowerlifting;

public sealed class GetClientPowerliftingHandler(
    ICoachClientRepository coachClientRepo,
    IPowerliftingRepository powerliftingRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetClientPowerliftingQuery, ClientPowerliftingResponse>
{
    public async ValueTask<ClientPowerliftingResponse> Handle(
        GetClientPowerliftingQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var hasRelationship = await coachClientRepo
            .HasActiveRelationshipAsync(coachId, request.ClientId, cancellationToken);

        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not coach this user.");

        var (section, insights) = await PowerliftingSectionBuilder.BuildAsync(
            request.ClientId,
            (lift, ct) => powerliftingRepo.GetCompletedSetsForUserAsync(request.ClientId, lift, ct),
            powerliftingRepo,
            userManager,
            cancellationToken);

        return new ClientPowerliftingResponse(request.ClientId, section, insights);
    }
}
