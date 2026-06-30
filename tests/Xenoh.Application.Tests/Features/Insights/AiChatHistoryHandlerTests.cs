using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Commands.AiChat;
using Xenoh.Application.Features.Insights.Queries.AiChat;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class AiChatHistoryHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task ListConversations_ReturnsOnlyCurrentUserActiveConversations()
    {
        await using var db = CreateContext();
        var otherUserId = Guid.NewGuid();
        db.AiChatConversations.AddRange(
            NewConversation(UserId, "Mine"),
            NewConversation(UserId, "Archived", archived: true),
            NewConversation(otherUserId, "Other"));
        await db.SaveChangesAsync();

        var handler = new ListAiChatConversationsHandler(db, CurrentUser());

        var result = await handler.Handle(new ListAiChatConversationsQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Mine");
    }

    [Fact]
    public async Task GetMessages_WhenConversationBelongsToOtherUser_Throws()
    {
        await using var db = CreateContext();
        var conversation = NewConversation(Guid.NewGuid(), "Other");
        db.AiChatConversations.Add(conversation);
        await db.SaveChangesAsync();

        var handler = new GetAiChatMessagesHandler(db, CurrentUser());

        await FluentActions.Invoking(() =>
                handler.Handle(new GetAiChatMessagesQuery(conversation.Id), CancellationToken.None).AsTask())
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Conversation not found or access denied.");
    }

    [Fact]
    public async Task SendMessage_SavesBothMessages_AndSendsSummaryPlusRecentMessagesToAi()
    {
        await using var db = CreateContext();
        var conversation = NewConversation(UserId, "New chat");
        conversation.Summary = "Earlier context summary.";
        db.AiChatConversations.Add(conversation);

        for (var i = 0; i < 25; i++)
        {
            db.AiChatMessages.Add(new AiChatMessage
            {
                ConversationId = conversation.Id,
                Role = i % 2 == 0 ? AiChatMessageRole.User : AiChatMessageRole.Assistant,
                Content = $"Existing message {i}",
            });
        }
        await db.SaveChangesAsync();

        var ai = new StubUserAnalysisAi("Assistant reply");
        var handler = new SendAiChatMessageHandler(db, CurrentUser(), ai);

        var result = await handler.Handle(
            new SendAiChatMessageCommand(conversation.Id, "Should I add weight?", "en"),
            CancellationToken.None);

        result.UserMessage.Content.Should().Be("Should I add weight?");
        result.AssistantMessage.Content.Should().Be("Assistant reply");
        result.Conversation.Title.Should().Be("Should I add weight?");

        var savedMessages = await db.AiChatMessages.CountAsync(m => m.ConversationId == conversation.Id);
        savedMessages.Should().Be(27);

        ai.ChatRequests.Should().ContainSingle();
        ai.ChatRequests[0].ConversationSummary.Should().Be("Earlier context summary.");
        ai.ChatRequests[0].Messages.Should().HaveCount(20);
        ai.ChatRequests[0].Messages[^1].Content.Should().Be("Should I add weight?");
    }

    [Fact]
    public async Task SendMessage_WhenTwentyOlderMessagesAreUnsummarized_UpdatesSummaryOnce()
    {
        await using var db = CreateContext();
        var conversation = NewConversation(UserId, "Existing chat");
        db.AiChatConversations.Add(conversation);

        for (var i = 0; i < 40; i++)
        {
            db.AiChatMessages.Add(new AiChatMessage
            {
                ConversationId = conversation.Id,
                Role = i % 2 == 0 ? AiChatMessageRole.User : AiChatMessageRole.Assistant,
                Content = $"Message {i}",
            });
        }
        await db.SaveChangesAsync();

        var ai = new StubUserAnalysisAi("Assistant reply", "Compacted summary");
        var handler = new SendAiChatMessageHandler(db, CurrentUser(), ai);

        await handler.Handle(
            new SendAiChatMessageCommand(conversation.Id, "Next question", "en"),
            CancellationToken.None);

        var updated = await db.AiChatConversations.SingleAsync(c => c.Id == conversation.Id);
        updated.Summary.Should().Be("Compacted summary");
        updated.SummaryCutoffAt.Should().NotBeNull();
        ai.SummaryRequests.Should().ContainSingle();
        ai.SummaryRequests[0].Messages.Should().HaveCount(20);
    }

    private static AiChatConversation NewConversation(Guid userId, string title, bool archived = false) =>
        new()
        {
            UserId = userId,
            Title = title,
            LastMessageAt = DateTime.UtcNow,
            IsArchived = archived,
        };

    private sealed class StubUserAnalysisAi(
        string chatReply,
        string summary = "Summary"
    ) : IUserAnalysisAi
    {
        public List<CoachChatAiRequest> ChatRequests { get; } = [];
        public List<CoachChatSummaryAiRequest> SummaryRequests { get; } = [];

        public Task<CoachChatAiResult> ChatAsync(
            CoachChatAiRequest request,
            CancellationToken cancellationToken)
        {
            ChatRequests.Add(request);
            return Task.FromResult(new CoachChatAiResult(chatReply));
        }

        public Task<CoachChatSummaryAiResult> SummarizeCoachChatAsync(
            CoachChatSummaryAiRequest request,
            CancellationToken cancellationToken)
        {
            SummaryRequests.Add(request);
            return Task.FromResult(new CoachChatSummaryAiResult(summary));
        }

        public Task<UserAnalysisAiResult> GenerateAsync(UserAnalysisAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<StarterPlanAiResult> GenerateStarterPlanAsync(StarterPlanAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(PlanBalanceAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlanProgressInsightAiResult> GeneratePlanProgressInsightAsync(PlanProgressInsightAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(CoachClientBriefAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<TrainingCoachTipAiResult> GenerateTrainingCoachTipAsync(TrainingCoachTipAiRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
