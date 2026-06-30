namespace Xenoh.Application.Features.Insights.Dtos;

public sealed record AiChatConversationResponse(
    Guid Id,
    string Title,
    int MessageCount,
    DateTime LastMessageAt,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AiChatConversationPageResponse(
    IReadOnlyList<AiChatConversationResponse> Items,
    bool HasMore);

public sealed record AiChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record AiChatMessagePageResponse(
    IReadOnlyList<AiChatMessageResponse> Items,
    bool HasMore);

public sealed record SendAiChatMessageResponse(
    AiChatConversationResponse Conversation,
    AiChatMessageResponse UserMessage,
    AiChatMessageResponse AssistantMessage);
