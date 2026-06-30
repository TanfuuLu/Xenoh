using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Queries.AiChat;

public sealed class GetAiChatMessagesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetAiChatMessagesQuery, AiChatMessagePageResponse>
{
    public async ValueTask<AiChatMessagePageResponse> Handle(
        GetAiChatMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var ownsConversation = await db.AiChatConversations
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ConversationId && c.UserId == userId, cancellationToken);

        if (!ownsConversation)
            throw new InvalidOperationException("Conversation not found or access denied.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var query = db.AiChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId);

        if (request.Before.HasValue)
            query = query.Where(m => m.CreatedAt < request.Before.Value);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        items.Reverse();

        return new AiChatMessagePageResponse(
            items.Select(AiChatMapping.ToResponse).ToList(),
            hasMore);
    }
}
