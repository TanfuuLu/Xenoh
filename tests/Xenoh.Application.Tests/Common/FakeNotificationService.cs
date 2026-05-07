using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeNotificationService : INotificationService
{
    public List<NotificationCall> Calls { get; } = [];

    public Task NotifyAsync(
        Guid recipientId, string type, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        Calls.Add(new NotificationCall(recipientId, type, message, relatedEntityId, relatedEntityType));
        return Task.CompletedTask;
    }
}

public sealed record NotificationCall(
    Guid RecipientId,
    string Type,
    string Message,
    Guid? RelatedEntityId,
    string? RelatedEntityType);
