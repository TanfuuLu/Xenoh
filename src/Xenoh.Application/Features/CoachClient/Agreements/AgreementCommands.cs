using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Agreements;

public sealed class ProposeAgreementHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<ProposeAgreementCommand, AgreementDto>
{
    public async ValueTask<AgreementDto> Handle(ProposeAgreementCommand request, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships.SingleOrDefaultAsync(x => x.Id == request.RelationshipId
            && (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId), ct)
            ?? throw new KeyNotFoundException("Agreement not found.");
        if (!request.Acknowledged) throw new InvalidOperationException("Review and acknowledge the proposed terms.");
        if (r.Revision != request.ExpectedRevision) throw new AgreementConflictException("The agreement changed. Reload before proposing terms.");
        if (r.Status == RelationshipStatus.Ended || r.NoticeEndsAtUtc != null)
            throw new InvalidOperationException("An ending agreement cannot be extended. Start a new invitation after it ends.");
        if (r.AgreementId == null && currentUser.UserId != r.CoachId)
            throw new InvalidOperationException("Ask your coach to publish the first agreement.");
        var versions = await db.CoachingAgreements.Where(x => x.RelationshipId == r.Id).ToListAsync(ct);
        if (versions.Any(x => x.AcceptedAtUtc == null && x.RejectedAtUtc == null))
            throw new AgreementConflictException("Respond to the pending proposal before creating another.");
        CoachingAgreement proposal;
        var current = versions.SingleOrDefault(x => x.Id == r.AgreementId);
        if (current != null)
        {
            // Renewal extends the existing scope; changing scope requires a new agreement.
            if (request.EndDate <= r.EndDate || request.EndDate <= CoachingPolicy.LocalDate(DateTime.UtcNow) || request.EndDate.Year > 2100)
                throw new InvalidOperationException("The renewal must end after the current agreement.");
            var terms = current.Adapt<AgreementTerms>();
            var start = CoachingPolicy.HasAccess(r, DateTime.UtcNow) || r.StartDate > CoachingPolicy.LocalDate(DateTime.UtcNow)
                ? r.StartDate : CoachingPolicy.LocalDate(DateTime.UtcNow);
            proposal = terms.Adapt<CoachingAgreement>();
            proposal.CoachId = r.CoachId;
            proposal.StartDate = start;
            proposal.EndDate = request.EndDate;
            proposal.PublishedBy = currentUser.UserId;
            proposal.PublishedAtUtc = DateTime.UtcNow;
        }
        else
            proposal = request.Terms.Publish(r.CoachId, request.StartDate, request.EndDate, currentUser.UserId);
        proposal.RelationshipId = r.Id;
        proposal.Version = versions.Count == 0 ? 1 : versions.Max(x => x.Version) + 1;
        db.CoachingAgreements.Add(proposal);
        r.Revision = Guid.NewGuid();
        AgreementEvents.Record(db, r, "Proposed", currentUser.UserId, null, proposal.Id);
        await db.SaveChangesAsync(ct);
        await notifications.DeliverPendingAgreementNotificationsAsync(ct);
        return proposal.Adapt<AgreementDto>();
    }
}

public sealed class RespondToAgreementHandler(IApplicationDbContext db, ICurrentUserService currentUser,
    ISubscriptionService subscriptions, INotificationService notifications) : IRequestHandler<RespondToAgreementCommand>
{
    public async ValueTask<Unit> Handle(RespondToAgreementCommand request, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships.Include(x => x.Coach).SingleOrDefaultAsync(x => x.Id == request.RelationshipId
            && (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId), ct)
            ?? throw new KeyNotFoundException("Agreement not found.");
        var proposal = await db.CoachingAgreements.SingleOrDefaultAsync(x => x.Id == request.AgreementId && x.RelationshipId == r.Id, ct)
            ?? throw new KeyNotFoundException("Proposal not found.");
        if (request.Accept && proposal.AcceptedBy == currentUser.UserId && proposal.AcceptedAtUtc != null) return Unit.Value;
        if (!request.Accept && proposal.RejectedAtUtc != null) return Unit.Value;
        if (r.Revision != request.ExpectedRevision || proposal.AcceptedAtUtc != null || proposal.RejectedAtUtc != null)
            throw new AgreementConflictException("The proposal changed. Reload the agreement.");
        if (request.Accept)
        {
            if (!request.Acknowledged || proposal.PublishedBy == currentUser.UserId)
                throw new InvalidOperationException("The other participant must review and accept the proposal.");
            var today = CoachingPolicy.LocalDate(DateTime.UtcNow);
            if (r.NoticeEndsAtUtc != null || r.Status == RelationshipStatus.Ended || proposal.EndDate < today)
                throw new AgreementConflictException("This proposal can no longer be accepted.");
            if (await db.CoachClientRelationships.AnyAsync(x => x.ClientId == r.ClientId && x.Id != r.Id
                && x.Status != RelationshipStatus.Ended && x.Status != RelationshipStatus.Expired, ct))
                throw new AgreementConflictException("The client already has another reserved or active coach.");
            var capacity = await subscriptions.GetMaxClientsAsync(r.CoachId, ct);
            var reserved = await db.CoachClientRelationships.CountAsync(x => x.CoachId == r.CoachId && x.Id != r.Id
                && x.Status != RelationshipStatus.Ended && x.Status != RelationshipStatus.Expired
                && x.StartDate <= proposal.EndDate && (x.EndDate == null || x.EndDate >= proposal.StartDate), ct);
            if (capacity != int.MaxValue && reserved >= capacity) throw new InvalidOperationException("The coach has no available capacity for these dates.");
            r.Coach.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            proposal.AcceptedBy = currentUser.UserId;
            proposal.AcceptedAtUtc = DateTime.UtcNow;
            r.AgreementId = proposal.Id;
            r.StartDate = proposal.StartDate;
            r.EndDate = proposal.EndDate;
            r.NoticeDays = proposal.NoticeDays;
            r.EndedAtUtc = null;
            r.Status = RelationshipStatus.Active;
            r.RenewalRequestedBy = null;
            r.ProposedEndDate = null;
            AgreementEvents.Record(db, r, "Accepted", currentUser.UserId);
        }
        else
        {
            proposal.RejectedAtUtc = DateTime.UtcNow;
            AgreementEvents.Record(db, r, "Rejected", currentUser.UserId, null, proposal.Id);
        }
        r.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        await notifications.DeliverPendingAgreementNotificationsAsync(ct);
        return Unit.Value;
    }
}
