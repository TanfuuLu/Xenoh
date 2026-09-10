using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Services;

public sealed class NotificationService(
    IApplicationDbContext db,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task DeliverPendingAgreementNotificationsAsync(CancellationToken ct = default)
    {
        try
        {
            var pending = await db.Notifications.Where(n => n.SourceEventId != null && n.DeliveredAtUtc == null)
                .OrderBy(n => n.CreatedAt).Take(100).ToListAsync(ct);
            foreach (var notification in pending)
            {
                await hubContext.Clients.Group($"user-{notification.RecipientId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.Id, notification.Type, notification.Message, notification.IsRead,
                        notification.RelatedEntityId, notification.RelatedEntityType, notification.CreatedAt
                    }, ct);
                notification.DeliveredAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The agreement is already committed. The worker retries the durable outbox.
            logger.LogWarning(ex, "Agreement notification delivery deferred for retry.");
        }
    }

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
