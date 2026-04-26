using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;

public sealed class AcceptRequestHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<AcceptRequestCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForCoachAsync(request.RelationshipId, coachId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found.");

        if (relationship.Status != RelationshipStatus.Pending)
            throw new InvalidOperationException("Request has already been processed.");

        relationship.Status = RelationshipStatus.Active;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var coach = await userManager.FindByIdAsync(coachId.ToString());

        return new CoachRelationshipResponse(
            relationship.Id,
            relationship.ClientId,
            $"{relationship.Client.FirstName} {relationship.Client.LastName}",
            coachId,
            $"{coach!.FirstName} {coach.LastName}",
            relationship.Status.ToString(),
            relationship.CreatedAt
        );
    }
}
