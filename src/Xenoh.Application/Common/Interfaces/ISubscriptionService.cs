using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<PlanTier> GetActiveTierAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetMaxPlansAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetMaxClientsAsync(Guid coachId, CancellationToken ct = default);
    Task<bool> CanUseAdvancedAnalyticsAsync(Guid userId, CancellationToken ct = default);
}
