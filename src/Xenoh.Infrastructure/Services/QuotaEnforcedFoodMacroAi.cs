using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class QuotaEnforcedFoodMacroAi(
    OpenAiFoodMacroAi inner,
    IAiQuotaService quotaService
) : IFoodMacroAi
{
    public async Task<FoodMacroAiResult> ResolveAsync(
        FoodMacroAiRequest request,
        CancellationToken cancellationToken)
    {
        await quotaService.ConsumeAsync("food-macro", cancellationToken);
        return await inner.ResolveAsync(request, cancellationToken);
    }
}
