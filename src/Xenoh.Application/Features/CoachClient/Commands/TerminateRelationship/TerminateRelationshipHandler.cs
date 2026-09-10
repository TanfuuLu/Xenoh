using Mediator;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;

public sealed class TerminateRelationshipHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    IMediator mediator
) : IRequestHandler<TerminateRelationshipCommand>
{
    public async ValueTask<Unit> Handle(TerminateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.Pending)
            throw new InvalidOperationException("Only pending join requests can be declined this way. Use POST /api/coach-client/{id}/end to disconnect an established relationship.");

        await mediator.Send(new EndRelationshipCommand { RelationshipId = relationship.Id }, cancellationToken);

        return Unit.Value;
    }
}
