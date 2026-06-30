using FluentAssertions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Insights.Commands.CoachChat;
using Xenoh.Application.Tests.Common;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class CoachChatHandlerTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WhenQuestionClearlyOffTopic_ReturnsScopeReplyWithoutCallingAi()
    {
        await using var db = CreateContext();
        var ai = new StubUserAnalysisAi("unused");
        var handler = new CoachChatHandler(db, CurrentUser(), ai);

        var response = await handler.Handle(new CoachChatCommand
        {
            Language = "vi",
            Messages =
            [
                new CoachChatMessageDto("user", "Viết code Python đọc giá Bitcoin giúp tôi.")
            ]
        }, CancellationToken.None);

        response.Reply.Should().Contain("chỉ hỗ trợ");
        ai.ChatCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenAiReplyIsTooLong_TrimsResponse()
    {
        await using var db = CreateContext();
        var ai = new StubUserAnalysisAi(new string('a', 2000));
        var handler = new CoachChatHandler(db, CurrentUser(), ai);

        var response = await handler.Handle(new CoachChatCommand
        {
            Language = "en",
            Messages =
            [
                new CoachChatMessageDto("user", "How should I adjust my squat training this week?")
            ]
        }, CancellationToken.None);

        response.Reply.Length.Should().BeLessThanOrEqualTo(1603);
        response.Reply.Should().EndWith("...");
        ai.ChatCalls.Should().Be(1);
    }

    private sealed class StubUserAnalysisAi(string reply) : IUserAnalysisAi
    {
        public int ChatCalls { get; private set; }

        public Task<UserAnalysisAiResult> GenerateAsync(UserAnalysisAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StarterPlanAiResult> GenerateStarterPlanAsync(StarterPlanAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(PlanBalanceAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(CoachClientBriefAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlanProgressInsightAiResult> GeneratePlanProgressInsightAsync(PlanProgressInsightAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<TrainingCoachTipAiResult> GenerateTrainingCoachTipAsync(TrainingCoachTipAiRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CoachChatAiResult> ChatAsync(CoachChatAiRequest request, CancellationToken cancellationToken)
        {
            ChatCalls++;
            return Task.FromResult(new CoachChatAiResult(reply));
        }

        public Task<CoachChatSummaryAiResult> SummarizeCoachChatAsync(
            CoachChatSummaryAiRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
