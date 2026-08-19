using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;

public sealed class ConnectByInviteCodeHandler(
    IApplicationDbContext db,
    IPlanRepository planRepo,
    ISupplementRepository supplementRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<ConnectByInviteCodeCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(
        ConnectByInviteCodeCommand request, CancellationToken cancellationToken)
    {
        var clientId = currentUser.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Find the code
        var inviteCode = await db.CoachInviteCodes
            .Include(c => c.Coach)
            .FirstOrDefaultAsync(c => c.Code == request.Code.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException("Coach code not found.");

        // 2. Prevent self-coaching (a coach redeeming their own code)
        if (inviteCode.CoachId == clientId)
            throw new InvalidOperationException("You cannot use your own coach code.");

        // 3. Check it hasn't been used
        if (inviteCode.IsUsed)
            throw new InvalidOperationException("This coach code has already been used.");

        // 4. Check it hasn't expired
        if (inviteCode.CoachingEndDate < today)
            throw new InvalidOperationException("This coach code has expired.");

        // 5. Check the client doesn't already have an active/pending relationship
        var existingRelationship = await db.CoachClientRelationships
            .FirstOrDefaultAsync(
                r => r.ClientId == clientId &&
                     r.Status != RelationshipStatus.Ended &&
                     r.Status != RelationshipStatus.Expired,
                cancellationToken);

        if (existingRelationship is not null)
            throw new InvalidOperationException("You already have an active coach or a pending request.");

        // Expired contracts do not block a new connection, but "my coach" resolves the
        // client's first non-Ended relationship — leaving them behind would keep pointing
        // the client at the old coach. Close them out as the new one starts.
        var expired = await db.CoachClientRelationships
            .Where(r => r.ClientId == clientId && r.Status == RelationshipStatus.Expired)
            .ToListAsync(cancellationToken);

        foreach (var stale in expired)
        {
            stale.Status = RelationshipStatus.Ended;
            stale.RenewalRequestedBy = null;
            stale.ProposedEndDate = null;
            stale.UpdatedAt = DateTime.UtcNow;

            // This path ends the relationship without going through EndRelationshipHandler,
            // so it has to run the same teardown - otherwise the previous coach's work
            // outlives the contract and follows the client to their new coach.
            await CoachResourceCleanup.StageAsync(
                db, planRepo, supplementRepo, clientId, stale.CoachId, cancellationToken);
        }

        // 6. Load client info
        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");

        // 7. Create relationship (Active immediately — no pending step for code path)
        var relationship = new CoachClientRelationship
        {
            ClientId = clientId,
            CoachId = inviteCode.CoachId,
            Status = RelationshipStatus.Active,
            StartDate = inviteCode.CoachingStartDate,
            EndDate = inviteCode.CoachingEndDate,
            CoachInviteCodeId = inviteCode.Id
        };

        db.CoachClientRelationships.Add(relationship);

        // 8. Mark the code as used
        inviteCode.IsUsed = true;
        inviteCode.UsedByClientId = clientId;
        inviteCode.UsedAt = DateTime.UtcNow;
        inviteCode.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return CoachRelationshipMapper.ToResponse(relationship, client, inviteCode.Coach);
    }
}
