using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Agreements;

public sealed record RespondToCoachEndingCommand(Guid RelationshipId, Guid ExpectedRevision, bool Accept) : IRequest;

public sealed class RespondToCoachEndingHandler(IApplicationDbContext db, ICurrentUserService currentUser,
    IMediator mediator, INotificationService notifications) : IRequestHandler<RespondToCoachEndingCommand>
{
    public async ValueTask<Unit> Handle(RespondToCoachEndingCommand request, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships.SingleOrDefaultAsync(x => x.Id == request.RelationshipId
            && (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId), ct)
            ?? throw new KeyNotFoundException("Relationship not found.");
        if (currentUser.UserId != r.ClientId)
            throw new UnauthorizedAccessException("Only the client can respond to a coach's ending request.");
        if (r.Revision != request.ExpectedRevision || r.TerminationRequestedBy != r.CoachId
            || r.NoticeEndsAtUtc != null || r.Status is RelationshipStatus.Ended or RelationshipStatus.Expired)
            throw new AgreementConflictException("This ending request is no longer pending. Reload the relationship.");
        if (request.Accept)
        {
            var now = DateTime.UtcNow;
            await mediator.Send(new EndRelationshipCommand
            {
                RelationshipId = r.Id, ExpectedRevision = r.Revision, RequestedAtUtc = now,
                EffectiveAtUtc = CoachingPolicy.EndingAt(r, now, false)
            }, ct);
        }
        else
        {
            r.TerminationRequestedBy = null;
            r.Revision = Guid.NewGuid();
            r.UpdatedAt = DateTime.UtcNow;
            AgreementEvents.Record(db, r, "EndingApprovalDeclined", currentUser.UserId);
            await db.SaveChangesAsync(ct);
            await notifications.DeliverPendingAgreementNotificationsAsync(ct);
        }
        return Unit.Value;
    }
}
