using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient.Agreements;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Commands.EndRelationship;

public sealed class EndRelationshipHandler(
    ICoachClientRepository coachClientRepo,
    IPlanRepository planRepo,
    ISupplementRepository supplementRepo,
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<EndRelationshipCommand>
{
    public async ValueTask<Unit> Handle(EndRelationshipCommand request, CancellationToken cancellationToken)
    {
        _ = userManager; // Retain constructor compatibility with the existing handler tests.
        var relationship = await coachClientRepo.FindByIdForParticipantAsync(request.RelationshipId, currentUser.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Agreement not found.");
        if (currentUser.UserId == relationship.CoachId && request.Immediate && relationship.Status != RelationshipStatus.Pending)
            throw new UnauthorizedAccessException("Only the client can end coaching without the other participant's approval.");
        if (relationship.Status is RelationshipStatus.Ended or RelationshipStatus.Expired) return Unit.Value;
        if (!request.Immediate && relationship.NoticeEndsAtUtc != null) return Unit.Value;

        if (currentUser.UserId == relationship.CoachId && relationship.Status != RelationshipStatus.Pending)
        {
            if (relationship.TerminationRequestedBy == relationship.CoachId) return Unit.Value;
            if (request.ExpectedRevision is { } revision && revision != relationship.Revision)
                throw new AgreementConflictException("The relationship changed. Reload before requesting an ending.");
            relationship.TerminationRequestedBy = relationship.CoachId;
            relationship.Revision = Guid.NewGuid();
            relationship.UpdatedAt = DateTime.UtcNow;
            AgreementEvents.Record(db, relationship, "EndingApprovalRequested", currentUser.UserId);
            await db.SaveChangesAsync(cancellationToken);
            await notificationService.DeliverPendingAgreementNotificationsAsync(cancellationToken);
            return Unit.Value;
        }

        var now = DateTime.UtcNow;
        var requestedAt = request.RequestedAtUtc ?? now;
        if (relationship.AgreementId != null)
        {
            if (request.ExpectedRevision != relationship.Revision)
                throw new AgreementConflictException("The agreement changed. Review the ending date again.");
            if (!request.Immediate && (requestedAt > now || requestedAt < now.AddMinutes(-5) ||
                request.EffectiveAtUtc != CoachingPolicy.EndingAt(relationship, requestedAt, false)))
                throw new AgreementConflictException("The ending preview expired. Review the ending date again.");
        }
        var effectiveAt = CoachingPolicy.EndingAt(relationship, request.Immediate ? now : requestedAt, request.Immediate);
        relationship.TerminationRequestedBy = currentUser.UserId;
        relationship.NoticeEndsAtUtc = effectiveAt;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.Revision = Guid.NewGuid();
        relationship.UpdatedAt = now;
        foreach (var proposal in await db.CoachingAgreements.Where(a => a.RelationshipId == relationship.Id && a.AcceptedAtUtc == null && a.RejectedAtUtc == null).ToListAsync(cancellationToken))
            proposal.RejectedAtUtc = now;
        if (effectiveAt <= now)
        {
            if (relationship.Status != RelationshipStatus.Pending)
                await CoachResourceCleanup.StageAsync(db, planRepo, supplementRepo, relationship.ClientId, relationship.CoachId, cancellationToken);
            relationship.Status = RelationshipStatus.Ended;
            relationship.EndedAtUtc = effectiveAt;
            // Safety details are deliberately absent from the shared event and notification.
            AgreementEvents.Record(db, relationship, "Ended", currentUser.UserId, effectiveAt);
        }
        else
        {
            relationship.Status = RelationshipStatus.PendingTermination;
            AgreementEvents.Record(db, relationship, "EndingRequested", currentUser.UserId, effectiveAt);
        }
        await db.SaveChangesAsync(cancellationToken);
        await notificationService.DeliverPendingAgreementNotificationsAsync(cancellationToken);
        return Unit.Value;
    }
}
