using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

public sealed class RequestCoachHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RequestCoachCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(RequestCoachCommand request, CancellationToken cancellationToken)
    {
        var clientId = currentUser.UserId;

        var existing = await context.CoachClientRelationships
            .FirstOrDefaultAsync(r => r.ClientId == clientId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("You already have a coach or a pending request.");

        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");

        var coachRoles = await userManager.GetRolesAsync(coach);
        if (!coachRoles.Contains(UserRole.Coach))
            throw new InvalidOperationException("The specified user is not a coach.");

        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");

        var relationship = new CoachClientRelationship
        {
            ClientId = clientId,
            CoachId = request.CoachId,
            Status = RelationshipStatus.Pending
        };

        context.CoachClientRelationships.Add(relationship);
        await context.SaveChangesAsync(cancellationToken);

        return new CoachRelationshipResponse(
            relationship.Id,
            clientId,
            $"{client.FirstName} {client.LastName}",
            request.CoachId,
            $"{coach.FirstName} {coach.LastName}",
            relationship.Status.ToString(),
            relationship.CreatedAt
        );
    }
}
