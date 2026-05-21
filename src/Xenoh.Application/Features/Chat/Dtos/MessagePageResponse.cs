namespace Xenoh.Application.Features.Chat.Dtos;

public sealed record MessagePageResponse(
    IReadOnlyList<MessageResponse> Items,
    bool HasMore,
    int TotalUnread);
