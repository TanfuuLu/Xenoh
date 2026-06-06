using Mediator;

namespace Xenoh.Application.Features.Insights.Commands.CoachChat;

public sealed record CoachChatMessageDto(string Role, string Content);

public sealed record CoachChatCommand : IRequest<CoachChatResponse>
{
    public string? Language { get; init; }
    public required IReadOnlyList<CoachChatMessageDto> Messages { get; init; }
}

public sealed record CoachChatResponse(string Reply);
