using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;

public sealed class TerminateRelationshipHandler(
    ICoachClientRepository coachClientRepo,
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<TerminateRelationshipCommand>
{
    public async ValueTask<Unit> Handle(TerminateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status == RelationshipStatus.Active)
        {
            await planRepo.DeleteCoachPlansForClientAsync(
                relationship.ClientId, relationship.CoachId, cancellationToken);

            relationship.Status = RelationshipStatus.Ended;
            relationship.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            coachClientRepo.Remove(relationship);
        }

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
