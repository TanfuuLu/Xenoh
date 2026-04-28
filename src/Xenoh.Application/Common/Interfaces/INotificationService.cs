namespace Xenoh.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Guid recipientId,
        string type,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);
}
