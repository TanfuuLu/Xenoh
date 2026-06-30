using Mediator;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed record SendAiChatMessageCommand(
    Guid ConversationId,
    string Content,
    string? Language
) : IRequest<SendAiChatMessageResponse>;
