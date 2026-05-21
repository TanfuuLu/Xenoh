using Mediator;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Queries.GetMessages;

public sealed record GetMessagesQuery(
    Guid RelationshipId,
    int PageSize = 30,
    DateTime? Before = null
) : IRequest<MessagePageResponse>;
