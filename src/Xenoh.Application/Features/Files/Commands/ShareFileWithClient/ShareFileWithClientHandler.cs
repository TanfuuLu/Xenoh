using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Files.Dtos;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Files.Commands.ShareFileWithClient;

public sealed class ShareFileWithClientHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<ShareFileWithClientCommand, FileShareTargetDto>
{
    public async ValueTask<FileShareTargetDto> Handle(
        ShareFileWithClientCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var tier = await subscriptionService.GetActiveTierAsync(coachId, cancellationToken);
        if (tier is not (PlanTier.ProCoach or PlanTier.Organizer))
            throw new InvalidOperationException("Only coaches can share files with clients.");

        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.OwnerId == coachId, cancellationToken)
            ?? throw new InvalidOperationException("File not found or access denied.");

        var isActiveClient = await db.CoachClientRelationships.AnyAsync(
            r => r.CoachId == coachId
                 && r.ClientId == request.ClientId
                 && r.Status == RelationshipStatus.Active,
            cancellationToken);
        if (!isActiveClient)
            throw new InvalidOperationException("This user is not one of your active clients.");

        var client = await db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == request.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("Client not found.");

        var clientName = $"{client.FirstName} {client.LastName}".Trim();

        // Idempotent: if already shared, return the existing share.
        var existing = await db.StoredFileShares.FirstOrDefaultAsync(
            s => s.FileId == file.Id && s.SharedWithUserId == request.ClientId, cancellationToken);
        if (existing is not null)
            return new FileShareTargetDto(existing.Id, request.ClientId, clientName);

        var share = new StoredFileShare
        {
            FileId = file.Id,
            SharedByUserId = coachId,
            SharedWithUserId = request.ClientId
        };

        db.StoredFileShares.Add(share);
        await db.SaveChangesAsync(cancellationToken);

        return new FileShareTargetDto(share.Id, request.ClientId, clientName);
    }
}
