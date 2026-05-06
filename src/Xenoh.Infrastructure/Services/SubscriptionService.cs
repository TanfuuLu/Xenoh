using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Services;

public sealed class SubscriptionService(ISubscriptionRepository subscriptionRepo) : ISubscriptionService
{
    // TEMP TEST BYPASS: disable subscription limits while validating features.
    // Remove this flag and the early returns below to restore paid-plan checks.
    private static readonly bool TemporaryFeatureBypass = true;

    public async Task<PlanTier> GetActiveTierAsync(Guid userId, CancellationToken ct = default)
    {
        if (TemporaryFeatureBypass)
            return PlanTier.ProCoach;

        var sub = await subscriptionRepo.GetByUserIdAsNoTrackingAsync(userId, ct);
        if (sub is null || !sub.IsActive) return PlanTier.Free;
        return sub.Tier;
    }

    public async Task<int> GetMaxPlansAsync(Guid userId, CancellationToken ct = default)
    {
        if (TemporaryFeatureBypass)
            return int.MaxValue;

        var tier = await GetActiveTierAsync(userId, ct);
        return SubscriptionLimits.MaxPlans(tier);
    }

    public async Task<int> GetMaxClientsAsync(Guid coachId, CancellationToken ct = default)
    {
        if (TemporaryFeatureBypass)
            return int.MaxValue;

        var tier = await GetActiveTierAsync(coachId, ct);
        return SubscriptionLimits.MaxClients(tier);
    }

    public async Task<bool> CanUseAdvancedAnalyticsAsync(Guid userId, CancellationToken ct = default)
    {
        if (TemporaryFeatureBypass)
            return true;

        var tier = await GetActiveTierAsync(userId, ct);
        return SubscriptionLimits.CanUseAdvancedAnalytics(tier);
    }
}
