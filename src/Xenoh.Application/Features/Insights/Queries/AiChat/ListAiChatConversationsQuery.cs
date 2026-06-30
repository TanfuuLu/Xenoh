using Mediator;
using Xenoh.Application.Features.Insights.Dtos;

namespace Xenoh.Application.Features.Insights.Queries.AiChat;

public sealed record ListAiChatConversationsQuery(
    DateTime? Cursor = null,
    int PageSize = 20
) : IRequest<AiChatConversationPageResponse>;
