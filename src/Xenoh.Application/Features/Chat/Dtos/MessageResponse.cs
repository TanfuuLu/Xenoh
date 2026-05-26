namespace Xenoh.Application.Features.Chat.Dtos;

public sealed record MessageResponse(
    Guid Id,
    Guid RelationshipId,
    Guid SenderId,
    string SenderName,
    string Content,
    string Kind,
    bool IsRead,
    DateTime CreatedAt);
