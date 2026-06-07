using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class QuotaEnforcedUserAnalysisAi(
    OpenAiUserAnalysisAi inner,
    IAiQuotaService quotaService
) : IUserAnalysisAi
{
    public async Task<UserAnalysisAiResult> GenerateAsync(
        UserAnalysisAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("personal-insight", cancellationToken);
        return await inner.GenerateAsync(request, cancellationToken);
    }

    public async Task<StarterPlanAiResult> GenerateStarterPlanAsync(
        StarterPlanAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("starter-plan", cancellationToken);
        return await inner.GenerateStarterPlanAsync(request, cancellationToken);
    }

    public async Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(
        PlanBalanceAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("plan-balance", cancellationToken);
        return await inner.ReviewPlanBalanceAsync(request, cancellationToken);
    }

    public async Task<CoachClientBriefAiResult> GenerateCoachClientBriefAsync(
        CoachClientBriefAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("client-brief", cancellationToken);
        return await inner.GenerateCoachClientBriefAsync(request, cancellationToken);
    }

    public async Task<TrainingCoachTipAiResult> GenerateTrainingCoachTipAsync(
        TrainingCoachTipAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("coach-tip", cancellationToken);
        return await inner.GenerateTrainingCoachTipAsync(request, cancellationToken);
    }

    public async Task<CoachChatAiResult> ChatAsync(
        CoachChatAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("coach-chat", cancellationToken);
        return await inner.ChatAsync(request, cancellationToken);
    }
}
