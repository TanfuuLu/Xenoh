using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class QuotaEnforcedCycleInsightAi(
    OpenAiCycleInsightAi inner,
    IAiQuotaService quotaService
) : ICycleInsightAi
{
    public async Task<CycleInsightAiResult> GenerateAsync(
        CycleInsightAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("cycle-insight", cancellationToken);
        return await inner.GenerateAsync(request, cancellationToken);
    }
}
