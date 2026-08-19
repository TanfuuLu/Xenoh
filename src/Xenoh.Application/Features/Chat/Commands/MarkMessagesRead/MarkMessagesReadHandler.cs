using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Chat.Commands.MarkMessagesRead;

public sealed class MarkMessagesReadHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<MarkMessagesReadCommand>
{
    public async ValueTask<Unit> Handle(
        MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Without this the endpoint reads as "mark any relationship read": the update
        // below is keyed only on RelationshipId, so a non-participant could clear
        // someone else's unread badge.
        var isParticipant = await db.CoachClientRelationships
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == request.RelationshipId
                     && (r.ClientId == userId || r.CoachId == userId),
                cancellationToken);

        if (!isParticipant)
            throw new InvalidOperationException("Relationship not found or access denied.");

        await db.Messages
            .Where(m => m.RelationshipId == request.RelationshipId
                        && m.SenderId != userId
                        && !m.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsRead, true),
                cancellationToken);

        return Unit.Value;
    }
}
