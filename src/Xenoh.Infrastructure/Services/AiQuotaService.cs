using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Infrastructure.Services;

public sealed class AiQuotaService(
    ApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IAiQuotaService
{
    public async Task<AiQuotaSnapshot> ConsumeAsync(
        string feature,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var tier = await subscriptionService.GetActiveTierAsync(userId, cancellationToken);
        var limit = SubscriptionLimits.MaxAiRequestsPerMonth(tier);
        var periodStart = GetCurrentPeriodStart();

        if (limit <= 0)
            throw new InvalidOperationException("AI features are not included in your current plan.");

        var normalizedFeature = NormalizeFeature(feature);
        var usage = await GetOrCreateUsageAsync(userId, periodStart, cancellationToken);
        if (usage.UsedRequests >= limit)
            throw new InvalidOperationException($"Monthly AI quota reached ({limit}/{limit}). Upgrade or wait until next month.");

        usage.UsedRequests++;
        usage.LastFeature = normalizedFeature;
        usage.LastConsumedAt = DateTime.UtcNow;
        usage.UpdatedAt = DateTime.UtcNow;

        var featureUsage = await GetOrCreateFeatureUsageAsync(
            userId,
            periodStart,
            normalizedFeature,
            cancellationToken);

        featureUsage.UsedRequests++;
        featureUsage.LastConsumedAt = usage.LastConsumedAt;
        featureUsage.UpdatedAt = usage.UpdatedAt;

        await db.SaveChangesAsync(cancellationToken);

        return ToSnapshot(tier, limit, usage, periodStart);
    }

    public async Task<AiQuotaSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var tier = await subscriptionService.GetActiveTierAsync(userId, cancellationToken);
        var limit = SubscriptionLimits.MaxAiRequestsPerMonth(tier);
        var periodStart = GetCurrentPeriodStart();

        var usage = await db.AiUsageQuotas
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.UserId == userId && q.PeriodStart == periodStart, cancellationToken);

        return new AiQuotaSnapshot(
            tier,
            limit,
            usage?.UsedRequests ?? 0,
            Math.Max(0, limit - (usage?.UsedRequests ?? 0)),
            periodStart);
    }

    private async Task<AiUsageQuota> GetOrCreateUsageAsync(
        Guid userId,
        DateOnly periodStart,
        CancellationToken cancellationToken)
    {
        var usage = await db.AiUsageQuotas
            .FirstOrDefaultAsync(q => q.UserId == userId && q.PeriodStart == periodStart, cancellationToken);

        if (usage is not null)
            return usage;

        usage = new AiUsageQuota
        {
            UserId = userId,
            PeriodStart = periodStart,
        };

        db.AiUsageQuotas.Add(usage);
        return usage;
    }

    private async Task<AiFeatureUsage> GetOrCreateFeatureUsageAsync(
        Guid userId,
        DateOnly periodStart,
        string feature,
        CancellationToken cancellationToken)
    {
        var usage = await db.AiFeatureUsages
            .FirstOrDefaultAsync(
                u => u.UserId == userId && u.PeriodStart == periodStart && u.Feature == feature,
                cancellationToken);

        if (usage is not null)
            return usage;

        usage = new AiFeatureUsage
        {
            UserId = userId,
            PeriodStart = periodStart,
            Feature = feature,
        };

        db.AiFeatureUsages.Add(usage);
        return usage;
    }

    private static DateOnly GetCurrentPeriodStart()
    {
        var now = DateTime.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }

    private static string NormalizeFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            return "unknown";

        var trimmed = feature.Trim();
        return trimmed.Length <= 64 ? trimmed : trimmed[..64];
    }

    private static AiQuotaSnapshot ToSnapshot(
        PlanTier tier,
        int limit,
        AiUsageQuota usage,
        DateOnly periodStart) =>
        new(tier, limit, usage.UsedRequests, Math.Max(0, limit - usage.UsedRequests), periodStart);
}
