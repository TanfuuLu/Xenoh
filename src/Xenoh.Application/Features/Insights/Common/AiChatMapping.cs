using Xenoh.Application.Features.Insights.Dtos;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Insights.Common;

internal static class AiChatMapping
{
    public static AiChatConversationResponse ToResponse(AiChatConversation c) =>
        new(
            c.Id,
            string.IsNullOrWhiteSpace(c.Title) ? "New chat" : c.Title,
            c.MessageCount,
            c.LastMessageAt,
            c.IsArchived,
            c.CreatedAt,
            c.UpdatedAt);

    public static AiChatMessageResponse ToResponse(AiChatMessage m) =>
        new(
            m.Id,
            m.ConversationId,
            m.Role == Domain.Enums.AiChatMessageRole.Assistant ? "assistant" : "user",
            m.Content,
            m.CreatedAt);
}
