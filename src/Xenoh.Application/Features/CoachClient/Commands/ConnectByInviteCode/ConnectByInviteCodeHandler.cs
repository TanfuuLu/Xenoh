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

namespace Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;

public sealed class ConnectByInviteCodeHandler(
    IApplicationDbContext db,
    IPlanRepository planRepo,
    ISupplementRepository supplementRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    ISubscriptionService subscriptionService,
    INotificationService notifications
) : IRequestHandler<ConnectByInviteCodeCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(ConnectByInviteCodeCommand request, CancellationToken ct)
    {
        var clientId = currentUser.UserId;
        var now = DateTime.UtcNow;
        var invite = await db.CoachInviteCodes.Include(c => c.Coach).Include(c => c.Agreement)
            .SingleOrDefaultAsync(c => c.Code == request.Code.Trim().ToUpperInvariant(), ct)
            ?? throw new InvalidOperationException("Invitation not found.");
        if (!request.Acknowledged || request.AgreementId == Guid.Empty || invite.AgreementId != request.AgreementId || invite.Agreement is null)
            throw new AgreementConflictException("Preview and accept the exact published agreement before connecting.");
        if (invite.CoachId == clientId) throw new InvalidOperationException("You cannot accept your own invitation.");

        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");
        if (invite.IsUsed)
        {
            if (invite.UsedByClientId != clientId) throw new AgreementConflictException("This invitation has already been accepted.");
            var existing = await db.CoachClientRelationships.AsNoTracking().SingleAsync(r => r.CoachInviteCodeId == invite.Id, ct);
            return CoachRelationshipMapper.ToResponse(existing, client, invite.Coach);
        }
        if (invite.CoachingEndDate < CoachingPolicy.LocalDate(now))
            throw new AgreementConflictException("This invitation has expired.");

        var previous = await db.CoachClientRelationships.Where(r => r.ClientId == clientId && r.Status != RelationshipStatus.Ended).ToListAsync(ct);
        foreach (var stale in previous)
        {
            if (CoachingPolicy.DisplayStatus(stale, now) is not ("Expired" or "Ended"))
                throw new AgreementConflictException("You already have a coach or a reserved coaching agreement.");
            if (stale.Status != RelationshipStatus.Expired)
            {
                stale.Status = RelationshipStatus.Expired;
                stale.EndedAtUtc = stale.NoticeEndsAtUtc ?? (stale.EndDate is { } end ? CoachingPolicy.TermBoundary(end) : now);
                stale.Revision = Guid.NewGuid();
                AgreementEvents.Record(db, stale, "Expired", null, stale.EndedAtUtc);
            }
            await CoachResourceCleanup.StageAsync(db, planRepo, supplementRepo, clientId, stale.CoachId, ct);
        }

        var maxClients = await subscriptionService.GetMaxClientsAsync(invite.CoachId, ct);
        var reserved = await db.CoachClientRelationships.AsNoTracking().CountAsync(r => r.CoachId == invite.CoachId
            && r.Status != RelationshipStatus.Ended && r.Status != RelationshipStatus.Expired
            && r.StartDate <= invite.CoachingEndDate && (r.EndDate == null || r.EndDate >= invite.CoachingStartDate), ct);
        if (maxClients != int.MaxValue && reserved >= maxClients)
            throw new InvalidOperationException("This coach has no available client capacity for the agreement dates.");
        // EF Identity concurrency token serializes competing redemptions against this coach's capacity.
        invite.Coach.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        var agreement = invite.Agreement;
        var relationship = new CoachClientRelationship
        {
            ClientId = clientId, CoachId = invite.CoachId, Status = RelationshipStatus.Active,
            StartDate = agreement.StartDate, EndDate = agreement.EndDate, NoticeDays = agreement.NoticeDays,
            AgreementId = agreement.Id, CoachInviteCodeId = invite.Id
        };
        db.CoachClientRelationships.Add(relationship);
        agreement.RelationshipId = relationship.Id;
        agreement.AcceptedBy = clientId;
        agreement.AcceptedAtUtc = now;
        invite.IsUsed = true;
        invite.UsedByClientId = clientId;
        invite.UsedAt = now;
        invite.UpdatedAt = now;
        AgreementEvents.Record(db, relationship, "Accepted", clientId);
        await db.SaveChangesAsync(ct);
        await notifications.DeliverPendingAgreementNotificationsAsync(ct);
        return CoachRelationshipMapper.ToResponse(relationship, client, invite.Coach);
    }
}
