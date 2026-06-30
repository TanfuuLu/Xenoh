using Mediator;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed record UpdateAiChatConversationCommand(
    Guid ConversationId,
    string? Title,
    bool? IsArchived
) : IRequest<AiChatConversationResponse>;
