namespace Xenoh.Application.Features.Notifications.Dtos;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Message,
    bool IsRead,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt);
