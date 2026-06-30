using Mediator;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Queries.AiChat;

public sealed record GetAiChatMessagesQuery(
    Guid ConversationId,
    DateTime? Before = null,
    int PageSize = 30
) : IRequest<AiChatMessagePageResponse>;
