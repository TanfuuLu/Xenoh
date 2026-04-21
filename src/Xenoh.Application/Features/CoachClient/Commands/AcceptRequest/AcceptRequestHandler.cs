using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;

public sealed class AcceptRequestHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<AcceptRequestCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var relationship = await context.CoachClientRelationships
            .Include(r => r.Client)
            .Include(r => r.Coach)
            .FirstOrDefaultAsync(r => r.Id == request.RelationshipId && r.CoachId == coachId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found.");

        if (relationship.Status != RelationshipStatus.Pending)
            throw new InvalidOperationException("Request has already been processed.");

        relationship.Status = RelationshipStatus.Accepted;
        relationship.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

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
