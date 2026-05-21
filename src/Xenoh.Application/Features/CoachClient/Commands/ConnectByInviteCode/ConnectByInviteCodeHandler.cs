using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;

public sealed class ConnectByInviteCodeHandler(
    IApplicationDbContext db,
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

        // 2. Check it hasn't been used
        if (inviteCode.IsUsed)
            throw new InvalidOperationException("This coach code has already been used.");

        // 3. Check it hasn't expired
        if (inviteCode.CoachingEndDate < today)
            throw new InvalidOperationException("This coach code has expired.");

        // 4. Check the client doesn't already have an active/pending relationship
        var existingRelationship = await db.CoachClientRelationships
            .FirstOrDefaultAsync(
                r => r.ClientId == clientId &&
                     r.Status != RelationshipStatus.Ended &&
                     r.Status != RelationshipStatus.Expired,
                cancellationToken);

        if (existingRelationship is not null)
            throw new InvalidOperationException("You already have an active coach or a pending request.");

        // 5. Load client info
        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");

        // 6. Create relationship (Active immediately — no pending step for code path)
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

        // 7. Mark the code as used
        inviteCode.IsUsed = true;
        inviteCode.UsedByClientId = clientId;
        inviteCode.UsedAt = DateTime.UtcNow;
        inviteCode.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return CoachRelationshipMapper.ToResponse(relationship, client, inviteCode.Coach);
    }
}
