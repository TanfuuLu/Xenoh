using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Common;
using Xenoh.Application.Features.Insights.Dtos;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Insights.Commands.AiChat;

public sealed class SendAiChatMessageHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<SendAiChatMessageCommand, SendAiChatMessageResponse>
{
    private const int RecentPromptMessageCount = 20;
    private const int SummaryBatchSize = 20;

    public async ValueTask<SendAiChatMessageResponse> Handle(
        SendAiChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var content = CoachChatSupport.TrimMessage(request.Content);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Message content is required.");

        var language = CoachChatSupport.NormalizeLanguage(request.Language);
        var conversation = await db.AiChatConversations
            .FirstOrDefaultAsync(
                c => c.Id == request.ConversationId && c.UserId == userId,
                cancellationToken);

        if (conversation is null)
            throw new InvalidOperationException("Conversation not found or access denied.");

        if (conversation.IsArchived)
            throw new InvalidOperationException("Cannot send messages to an archived conversation.");

        var userMessage = new AiChatMessage
        {
            ConversationId = conversation.Id,
            Role = AiChatMessageRole.User,
            Content = content,
        };

        db.AiChatMessages.Add(userMessage);
        await db.SaveChangesAsync(cancellationToken);

        string reply;
        if (CoachChatSupport.IsClearlyOffTopic(content))
        {
            reply = CoachChatSupport.OffTopicReply(language);
        }
        else
        {
            var snapshotJson = await CoachChatSupport.BuildSnapshotJsonAsync(db, userId, cancellationToken);
            var recentMessages = await LoadRecentPromptMessagesAsync(conversation.Id, cancellationToken);
            var result = await ai.ChatAsync(
                new CoachChatAiRequest(
                    language,
                    snapshotJson,
                    recentMessages,
                    conversation.Summary),
                cancellationToken);

            reply = CoachChatSupport.TrimReply(result.Reply);
        }

        var assistantMessage = new AiChatMessage
        {
            ConversationId = conversation.Id,
            Role = AiChatMessageRole.Assistant,
            Content = reply,
        };

        db.AiChatMessages.Add(assistantMessage);
        await db.SaveChangesAsync(cancellationToken);

        conversation.MessageCount = await db.AiChatMessages
            .AsNoTracking()
            .CountAsync(m => m.ConversationId == conversation.Id, cancellationToken);

        if (conversation.Title == "New chat")
            conversation.Title = BuildTitle(content);

        conversation.LastMessageAt = assistantMessage.CreatedAt;
        conversation.UpdatedAt = DateTime.UtcNow;
        await MaybeUpdateSummaryAsync(conversation, language, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new SendAiChatMessageResponse(
            AiChatMapping.ToResponse(conversation),
            AiChatMapping.ToResponse(userMessage),
            AiChatMapping.ToResponse(assistantMessage));
    }

    private async Task<List<CoachChatAiMessage>> LoadRecentPromptMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var messages = await db.AiChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(RecentPromptMessageCount)
            .Select(m => new CoachChatAiMessage(
                m.Role == AiChatMessageRole.Assistant ? "assistant" : "user",
                m.Content))
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages;
    }

    private async Task MaybeUpdateSummaryAsync(
        AiChatConversation conversation,
        string language,
        CancellationToken cancellationToken)
    {
        var latestMessages = await db.AiChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(RecentPromptMessageCount)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var latestSet = latestMessages.ToHashSet();
        var summaryQuery = db.AiChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id && !latestSet.Contains(m.Id));

        if (conversation.SummaryCutoffAt.HasValue)
            summaryQuery = summaryQuery.Where(m => m.CreatedAt > conversation.SummaryCutoffAt.Value);

        var candidates = await summaryQuery
            .OrderBy(m => m.CreatedAt)
            .Take(SummaryBatchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count < SummaryBatchSize)
            return;

        var summaryMessages = candidates
            .Select(m => new CoachChatAiMessage(
                m.Role == AiChatMessageRole.Assistant ? "assistant" : "user",
                m.Content))
            .ToList();

        var result = await ai.SummarizeCoachChatAsync(
            new CoachChatSummaryAiRequest(language, conversation.Summary, summaryMessages),
            cancellationToken);

        conversation.Summary = result.Summary.Length > 8000
            ? result.Summary[..8000]
            : result.Summary;
        conversation.SummaryCutoffAt = candidates[^1].CreatedAt;
    }

    private static string BuildTitle(string content)
    {
        var oneLine = string.Join(' ', content.Split(
            ['\r', '\n', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return oneLine.Length <= 60 ? oneLine : $"{oneLine[..57].TrimEnd()}...";
    }
}
