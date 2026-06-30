using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Queries.GetMessages;

public sealed class GetMessagesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetMessagesQuery, MessagePageResponse>
{
    public async ValueTask<MessagePageResponse> Handle(
        GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var memberCheck = await db.CoachClientRelationships
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == request.RelationshipId
                     && (r.ClientId == userId || r.CoachId == userId),
                cancellationToken);

        if (!memberCheck)
            throw new InvalidOperationException("Relationship not found or access denied.");

        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.RelationshipId == request.RelationshipId);

        if (request.Before.HasValue)
            query = query.Where(m => m.CreatedAt < request.Before.Value);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.PageSize + 1)
            .Select(m => new MessageResponse(
                m.Id,
                m.RelationshipId,
                m.SenderId,
                m.Sender.FirstName + " " + m.Sender.LastName,
                m.Content,
                m.Kind.ToString(),
                m.IsRead,
                m.Attachments
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new ChatAttachmentResponse(
                        a.Id,
                        a.FileName,
                        a.ContentType,
                        a.SizeBytes,
                        a.ContentType.StartsWith("image/")))
                    .ToList(),
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.PageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        items.Reverse();

        var totalUnread = await db.Messages
            .AsNoTracking()
            .CountAsync(
                m => m.RelationshipId == request.RelationshipId
                     && m.SenderId != userId
                     && !m.IsRead,
                cancellationToken);

        return new MessagePageResponse(items, hasMore, totalUnread);
    }
}
