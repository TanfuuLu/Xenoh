using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Queries.GetAttachmentUrl;

public sealed class GetAttachmentUrlHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService documentStorage
) : IRequestHandler<GetAttachmentUrlQuery, ChatAttachmentUrlResponse>
{
    public async ValueTask<ChatAttachmentUrlResponse> Handle(
        GetAttachmentUrlQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // The caller may read the attachment only if they are a participant in the
        // message's coach-client relationship.
        var attachment = await db.ChatMessageAttachments
            .AsNoTracking()
            .Where(a => a.Id == request.AttachmentId
                        && (a.Message.Relationship.ClientId == userId
                            || a.Message.Relationship.CoachId == userId))
            .Select(a => new { a.StorageKey, a.FileName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Attachment not found or access denied.");

        var url = await documentStorage.GetPresignedDownloadUrlAsync(
            attachment.StorageKey, attachment.FileName, cancellationToken, request.Inline);

        return new ChatAttachmentUrlResponse(url);
    }
}
