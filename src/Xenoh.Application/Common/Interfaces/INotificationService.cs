namespace Xenoh.Application.Common.Interfaces;

public interface INotificationService
{
    Task DeliverPendingAgreementNotificationsAsync(CancellationToken ct = default);
    Task NotifyAsync(
        Guid recipientId,
        string type,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);
}
