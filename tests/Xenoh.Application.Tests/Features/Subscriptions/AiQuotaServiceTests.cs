using FluentAssertions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class AiQuotaServiceTests : HandlerTestBase
{
    [Fact]
    public async Task ConsumeAsync_WhenFreeTier_Throws()
    {
        await using var db = CreateContext();
        var service = new AiQuotaService(
            db,
            CurrentUser(),
            new StubSubscriptionService(PlanTier.Free));

        Func<Task> act = () => service.ConsumeAsync("coach-chat");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI features are not included*");
    }

    [Fact]
    public async Task ConsumeAsync_WhenProIndividual_IncrementsCurrentMonthUsage()
    {
        await using var db = CreateContext();
        var service = new AiQuotaService(
            db,
            CurrentUser(),
            new StubSubscriptionService(PlanTier.ProIndividual));

        var snapshot = await service.ConsumeAsync("coach-chat");

        snapshot.Tier.Should().Be(PlanTier.ProIndividual);
        snapshot.MonthlyLimit.Should().Be(100);
        snapshot.UsedRequests.Should().Be(1);
        snapshot.RemainingRequests.Should().Be(99);
        db.AiUsageQuotas.Single().LastFeature.Should().Be("coach-chat");
        db.AiFeatureUsages.Single().Feature.Should().Be("coach-chat");
        db.AiFeatureUsages.Single().UsedRequests.Should().Be(1);
    }

    private sealed class StubSubscriptionService(PlanTier tier) : ISubscriptionService
    {
        public Task<PlanTier> GetActiveTierAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(tier);

        public Task<int> GetMaxPlansAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(int.MaxValue);

        public Task<int> GetMaxClientsAsync(Guid coachId, CancellationToken ct = default) =>
            Task.FromResult(int.MaxValue);

        public Task<bool> CanUseAdvancedAnalyticsAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(tier is PlanTier.ProIndividual or PlanTier.ProCoach);
    }
}
