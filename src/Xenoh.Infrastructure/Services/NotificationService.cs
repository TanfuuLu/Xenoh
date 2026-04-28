using Microsoft.AspNetCore.SignalR;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Services;

public sealed class NotificationService(
    IApplicationDbContext db,
    IHubContext<NotificationHub> hubContext
) : INotificationService
{
    public async Task NotifyAsync(
        Guid recipientId,
        string type,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            Type = type,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        await hubContext.Clients
            .Group($"user-{recipientId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Type,
                notification.Message,
                notification.IsRead,
                notification.RelatedEntityId,
                notification.RelatedEntityType,
                notification.CreatedAt
            }, ct);
    }
}
