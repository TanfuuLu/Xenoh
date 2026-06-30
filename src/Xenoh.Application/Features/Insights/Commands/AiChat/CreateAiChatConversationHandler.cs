using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;
using Xenoh.Application.Features.Insights.Dtos;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed class CreateAiChatConversationHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<CreateAiChatConversationCommand, AiChatConversationResponse>
{
    public async ValueTask<AiChatConversationResponse> Handle(
        CreateAiChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var now = DateTime.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? "New chat"
            : request.Title.Trim();

        var conversation = new AiChatConversation
        {
            UserId = userId,
            Title = title.Length > 120 ? title[..120] : title,
            LastMessageAt = now,
            UpdatedAt = now,
        };

        db.AiChatConversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);

        return AiChatMapping.ToResponse(conversation);
    }
}
