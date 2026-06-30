using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed class UpdateAiChatConversationHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateAiChatConversationCommand, AiChatConversationResponse>
{
    public async ValueTask<AiChatConversationResponse> Handle(
        UpdateAiChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var conversation = await db.AiChatConversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == userId, cancellationToken);

        if (conversation is null)
            throw new InvalidOperationException("Conversation not found or access denied.");

        if (request.Title is not null)
        {
            var title = request.Title.Trim();
            if (title.Length == 0)
                throw new InvalidOperationException("Conversation title cannot be empty.");

            conversation.Title = title.Length > 120 ? title[..120] : title;
        }

        if (request.IsArchived.HasValue)
            conversation.IsArchived = request.IsArchived.Value;

        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return AiChatMapping.ToResponse(conversation);
    }
}
