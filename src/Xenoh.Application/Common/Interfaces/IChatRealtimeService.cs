using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Common.Interfaces;

public interface IChatRealtimeService
{
    Task MessageSentAsync(
        Guid relationshipId,
        MessageResponse message,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default);
}
