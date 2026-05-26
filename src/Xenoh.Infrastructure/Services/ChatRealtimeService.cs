using Microsoft.AspNetCore.SignalR;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;
using Xenoh.Infrastructure.Hubs;

namespace Xenoh.Infrastructure.Services;

public sealed class ChatRealtimeService(
    IHubContext<NotificationHub> hubContext
) : IChatRealtimeService
{
    public Task MessageSentAsync(
        Guid relationshipId,
        MessageResponse message,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        var groupNames = recipientIds
            .Distinct()
            .Select(id => $"user-{id}")
            .ToList();

        if (groupNames.Count == 0)
            return Task.CompletedTask;

        return hubContext.Clients.Groups(groupNames).SendAsync(
            "ReceiveMessage",
            new
            {
                message.Id,
                message.RelationshipId,
                message.SenderId,
                message.SenderName,
                message.Content,
                message.Kind,
                message.IsRead,
                message.CreatedAt,
            },
            ct);
    }
}
