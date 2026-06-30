using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Chat.Commands.SendMessageWithAttachments;

public sealed class SendMessageWithAttachmentsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService documentStorage,
    IChatRealtimeService chatRealtimeService
) : IRequestHandler<SendMessageWithAttachmentsCommand, MessageResponse>
{
    private const long MaxFileBytes = 25 * 1024 * 1024;
    private const int MaxContentLength = 2000;

    public async ValueTask<MessageResponse> Handle(
        SendMessageWithAttachmentsCommand request, CancellationToken cancellationToken)
    {
        var content = (request.Content ?? string.Empty).Trim();
        var files = request.Files;

        if (content.Length == 0 && files.Count == 0)
            throw new InvalidOperationException("A message must have text or at least one attachment.");

        if (content.Length > MaxContentLength)
            throw new InvalidOperationException($"Message must be {MaxContentLength} characters or fewer.");

        foreach (var file in files)
        {
            if (file.Length <= 0)
                throw new InvalidOperationException($"'{file.FileName}' is empty.");
            if (file.Length > MaxFileBytes)
                throw new InvalidOperationException(
                    $"'{file.FileName}' is too large. Each file must be 25 MB or smaller.");
        }

        var senderId = currentUser.UserId;

        var relationship = await db.CoachClientRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == request.RelationshipId
                     && (r.ClientId == senderId || r.CoachId == senderId)
                     && r.Status == RelationshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Relationship not found or you are not a participant.");

        var sender = await db.ApplicationUsers
            .AsNoTracking()
            .FirstAsync(u => u.Id == senderId, cancellationToken);

        var message = new Message
        {
            RelationshipId = request.RelationshipId,
            SenderId = senderId,
            Content = content,
            IsRead = false,
        };

        // Upload each file to R2 (validates type by magic bytes) and record it.
        foreach (var file in files)
        {
            var storageKey = await documentStorage.SaveChatAttachmentAsync(
                senderId, file.FileName, file.ContentType, file.Content, cancellationToken);

            message.Attachments.Add(new ChatMessageAttachment
            {
                FileName = file.FileName,
                ContentType = CanonicalImageOrGiven(file.FileName, file.ContentType),
                SizeBytes = file.Length,
                StorageKey = storageKey,
            });
        }

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var senderName = $"{sender.FirstName} {sender.LastName}".Trim();

        var response = new MessageResponse(
            message.Id,
            message.RelationshipId,
            message.SenderId,
            senderName,
            message.Content,
            message.Kind.ToString(),
            message.IsRead,
            message.Attachments
                .Select(a => new ChatAttachmentResponse(
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.SizeBytes,
                    a.ContentType.StartsWith("image/")))
                .ToList(),
            message.CreatedAt);

        var recipientId = senderId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        await chatRealtimeService.MessageSentAsync(
            request.RelationshipId,
            response,
            [senderId, recipientId],
            cancellationToken);

        return response;
    }

    // Store a canonical content type derived from the extension so IsImage and inline
    // rendering are reliable even when the browser sends octet-stream.
    private static string CanonicalImageOrGiven(string fileName, string given)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => string.IsNullOrWhiteSpace(given) ? "application/octet-stream" : given,
        };
    }
}
