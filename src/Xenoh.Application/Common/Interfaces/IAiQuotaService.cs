using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces;

public sealed record AiQuotaSnapshot(
    PlanTier Tier,
    int MonthlyLimit,
    int UsedRequests,
    int RemainingRequests,
    DateOnly PeriodStart
);

public interface IAiQuotaService
{
    Task<AiQuotaSnapshot> ConsumeAsync(string feature, CancellationToken cancellationToken = default);
    Task<AiQuotaSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default);
}
