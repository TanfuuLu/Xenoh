using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeChatRealtimeService : IChatRealtimeService
{
    public List<ChatRealtimeCall> Calls { get; } = [];

    public Task MessageSentAsync(
        Guid relationshipId,
        MessageResponse message,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        Calls.Add(new ChatRealtimeCall(relationshipId, message, recipientIds));
        return Task.CompletedTask;
    }
}

public sealed record ChatRealtimeCall(
    Guid RelationshipId,
    MessageResponse Message,
    IReadOnlyCollection<Guid> RecipientIds);
