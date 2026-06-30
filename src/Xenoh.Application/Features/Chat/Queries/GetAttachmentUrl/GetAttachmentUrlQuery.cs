using Mediator;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Queries.GetAttachmentUrl;

public sealed record GetAttachmentUrlQuery(Guid AttachmentId, bool Inline)
    : IRequest<ChatAttachmentUrlResponse>;
