using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Queries.AiChat;

public sealed class ListAiChatConversationsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<ListAiChatConversationsQuery, AiChatConversationPageResponse>
{
    public async ValueTask<AiChatConversationPageResponse> Handle(
        ListAiChatConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var query = db.AiChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsArchived);

        if (request.Cursor.HasValue)
            query = query.Where(c => c.LastMessageAt < request.Cursor.Value);

        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return new AiChatConversationPageResponse(
            items.Select(AiChatMapping.ToResponse).ToList(),
            hasMore);
    }
}
