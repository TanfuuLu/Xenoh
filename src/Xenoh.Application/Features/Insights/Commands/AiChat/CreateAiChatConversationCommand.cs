using Mediator;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed record CreateAiChatConversationCommand(string? Title = null)
    : IRequest<AiChatConversationResponse>;
