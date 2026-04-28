using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeNotificationService : INotificationService
{
    public Task NotifyAsync(
        Guid recipientId, string type, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null,
        CancellationToken ct = default) => Task.CompletedTask;
}
