using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;

namespace Xenoh.Application.Features.Insights.Commands.CoachChat;

public sealed class CoachChatHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<CoachChatCommand, CoachChatResponse>
{
    private const int MaxMessages = 30;

    public async ValueTask<CoachChatResponse> Handle(
        CoachChatCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var messages = (request.Messages ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(MaxMessages)
            .Select(m => new CoachChatAiMessage(
                m.Role == "assistant" ? "assistant" : "user",
                CoachChatSupport.TrimMessage(m.Content)))
            .ToList();

        if (messages.Count == 0 || messages[^1].Role != "user")
            throw new InvalidOperationException("The last message must be from the user.");

        var language = CoachChatSupport.NormalizeLanguage(request.Language);
        if (CoachChatSupport.IsClearlyOffTopic(messages[^1].Content))
            return new CoachChatResponse(CoachChatSupport.OffTopicReply(language));

        var snapshotJson = await CoachChatSupport.BuildSnapshotJsonAsync(db, userId, cancellationToken);

        var result = await ai.ChatAsync(
            new CoachChatAiRequest(language, snapshotJson, messages),
            cancellationToken);

        return new CoachChatResponse(CoachChatSupport.TrimReply(result.Reply));
    }
}
