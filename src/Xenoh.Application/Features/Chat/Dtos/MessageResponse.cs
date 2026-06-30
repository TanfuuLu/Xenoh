namespace Xenoh.Application.Features.Chat.Dtos;

public sealed record ChatAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool IsImage);

public sealed record MessageResponse(
    Guid Id,
    Guid RelationshipId,
    Guid SenderId,
    string SenderName,
    string Content,
    string Kind,
    bool IsRead,
    IReadOnlyList<ChatAttachmentResponse> Attachments,
    DateTime CreatedAt);
