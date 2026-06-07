using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Admin;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.Admin;

public sealed class AdminQueryHandlerTests : HandlerTestBase
{
    private GetAdminDashboardHandler   CreateDashboardHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminUsersHandler       CreateUsersHandler(ApplicationDbContext ctx)     => new(ctx);
    private GetAdminPlansHandler       CreatePlansHandler(ApplicationDbContext ctx)     => new(ctx);
    private GetAdminUserDetailHandler  CreateUserDetailHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminSubscriptionsHandler CreateSubscriptionsHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminAiUsageSummaryHandler CreateAiUsageSummaryHandler(ApplicationDbContext ctx) => new(ctx);
    private GetReportSummaryHandler    CreateReportSummaryHandler(ApplicationDbContext ctx) => new(ctx);

    // ── GetAdminDashboard ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminDashboard_WhenEmpty_ReturnsZeroCounts()
    {
        await using var ctx = CreateContext();
        var result = await CreateDashboardHandler(ctx).Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.TotalUsers.Should().Be(0);
        result.PendingReports.Should().Be(0);
        result.TotalPlansCreated.Should().Be(0);
        result.ActivePaidSubscriptions.Should().Be(0);
    }

    [Fact]
    public async Task GetAdminDashboard_WithUsers_CountsCorrectly()
    {
        await SeedUserAsync();
        await SeedUserAsync();

        await using var ctx = CreateContext();
        var result = await CreateDashboardHandler(ctx).Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.TotalUsers.Should().Be(2);
    }

    // ── GetAdminUsers ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminUsers_WithNoFilter_ReturnsAllUsers()
    {
        await SeedUserAsync("alice@test.com");
        await SeedUserAsync("bob@test.com");

        await using var ctx = CreateContext();
        var result = await CreateUsersHandler(ctx).Handle(
            new GetAdminUsersQuery(null, null, null, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAdminUsers_WithEmailSearch_FiltersResults()
    {
        await SeedUserAsync("alice@test.com");
        await SeedUserAsync("bob@test.com");

        await using var ctx = CreateContext();
        var result = await CreateUsersHandler(ctx).Handle(
            new GetAdminUsersQuery("alice", null, null, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Email.Should().Contain("alice");
    }

    [Fact]
    public async Task GetAdminUsers_WithTierFilter_ReturnsPaidUsersOnly()
    {
        var freeUserId = Guid.NewGuid();
        var proUserId  = Guid.NewGuid();
        await SeedUserAsync(freeUserId, "free@test.com");
        await SeedUserAsync(proUserId,  "pro@test.com");

        await using var seedCtx = CreateContext();
        seedCtx.UserSubscriptions.Add(new UserSubscription
        {
            UserId = proUserId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateUsersHandler(ctx).Handle(
            new GetAdminUsersQuery(null, null, PlanTier.ProIndividual, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Email.Should().Contain("pro");
    }

    // ── GetAdminUserDetail ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminUserDetail_WhenUserExists_ReturnsDetail()
    {
        var userId = await SeedUserAsync("detail@test.com");

        await using var ctx = CreateContext();
        var result = await CreateUserDetailHandler(ctx).Handle(
            new GetAdminUserDetailQuery(userId), CancellationToken.None);

        result.Email.Should().Be("detail@test.com");
        result.Id.Should().Be(userId);
        result.PlanCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAdminUserDetail_WhenUserNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateUserDetailHandler(ctx).Handle(
            new GetAdminUserDetailQuery(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User not found.");
    }

    // ── GetAdminPlans ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminPlans_WithNoFilter_ReturnsAllPlans()
    {
        var ownerId = await SeedUserAsync("owner@test.com");
        await SeedPlanAsync(ownerId);
        await SeedPlanAsync(ownerId);

        await using var ctx = CreateContext();
        var result = await CreatePlansHandler(ctx).Handle(
            new GetAdminPlansQuery(null, null, null, null, null, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAdminPlans_WithOwnerIdFilter_ReturnsOwnerPlansOnly()
    {
        var owner1 = await SeedUserAsync("owner1@test.com");
        var owner2 = await SeedUserAsync("owner2@test.com");
        await SeedPlanAsync(owner1);
        await SeedPlanAsync(owner2);

        await using var ctx = CreateContext();
        var result = await CreatePlansHandler(ctx).Handle(
            new GetAdminPlansQuery(null, owner1, null, null, null, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].OwnerId.Should().Be(owner1);
    }

    [Fact]
    public async Task GetAdminPlans_WithActiveFilter_ReturnsActivePlansOnly()
    {
        var ownerId = await SeedUserAsync("owner3@test.com");
        await SeedPlanAsync(ownerId, isActive: true);
        await SeedPlanAsync(ownerId, isActive: false);

        await using var ctx = CreateContext();
        var result = await CreatePlansHandler(ctx).Handle(
            new GetAdminPlansQuery(null, null, null, null, null, true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeTrue();
    }

    // ── GetAdminSubscriptions ───────────────────────────────────────────────

    [Fact]
    public async Task GetAdminSubscriptions_WithNoFilter_ReturnsAll()
    {
        var u1 = await SeedUserAsync("sub1@test.com");
        var u2 = await SeedUserAsync("sub2@test.com");
        await using var seed = CreateContext();
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u1, Tier = PlanTier.ProIndividual, ExpiresAt = DateTime.UtcNow.AddDays(30) });
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u2, Tier = PlanTier.ProCoach,      ExpiresAt = DateTime.UtcNow.AddDays(60) });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateSubscriptionsHandler(ctx).Handle(
            new GetAdminSubscriptionsQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAdminSubscriptions_WithTierFilter_ReturnsMatchingTierOnly()
    {
        var u1 = await SeedUserAsync("pro1@test.com");
        var u2 = await SeedUserAsync("coach1@test.com");
        await using var seed = CreateContext();
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u1, Tier = PlanTier.ProIndividual, ExpiresAt = DateTime.UtcNow.AddDays(30) });
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u2, Tier = PlanTier.ProCoach,      ExpiresAt = DateTime.UtcNow.AddDays(60) });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateSubscriptionsHandler(ctx).Handle(
            new GetAdminSubscriptionsQuery(PlanTier.ProIndividual, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tier.Should().Be(PlanTier.ProIndividual);
    }

    // ── GetReportSummary ────────────────────────────────────────────────────

    [Fact]
    public async Task GetReportSummary_WhenEmpty_ReturnsTotalZero()
    {
        await using var ctx = CreateContext();
        var result = await CreateReportSummaryHandler(ctx).Handle(new GetReportSummaryQuery(), CancellationToken.None);

        result.Total.Should().Be(0);
        result.CountsByStatus.Should().BeEmpty();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminAiUsageSummary_ReturnsUsageByTierAndFeature()
    {
        var periodStart = new DateOnly(2026, 6, 1);
        var proUserId = await SeedUserAsync("ai-pro@test.com");
        var coachUserId = await SeedUserAsync("ai-coach@test.com");

        await using var seed = CreateContext();
        seed.UserSubscriptions.Add(new UserSubscription
        {
            UserId = proUserId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        seed.UserSubscriptions.Add(new UserSubscription
        {
            UserId = coachUserId,
            Tier = PlanTier.ProCoach,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        seed.AiUsageQuotas.AddRange(
            new AiUsageQuota
            {
                UserId = proUserId,
                PeriodStart = periodStart,
                UsedRequests = 3,
                LastFeature = "food-macro",
                LastConsumedAt = DateTime.UtcNow
            },
            new AiUsageQuota
            {
                UserId = coachUserId,
                PeriodStart = periodStart,
                UsedRequests = 5,
                LastFeature = "coach-client-ai-brief",
                LastConsumedAt = DateTime.UtcNow
            });
        seed.AiFeatureUsages.AddRange(
            new AiFeatureUsage
            {
                UserId = proUserId,
                PeriodStart = periodStart,
                Feature = "food-macro",
                UsedRequests = 3
            },
            new AiFeatureUsage
            {
                UserId = coachUserId,
                PeriodStart = periodStart,
                Feature = "coach-client-ai-brief",
                UsedRequests = 4
            },
            new AiFeatureUsage
            {
                UserId = coachUserId,
                PeriodStart = periodStart,
                Feature = "food-macro",
                UsedRequests = 1
            });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateAiUsageSummaryHandler(ctx).Handle(
            new GetAdminAiUsageSummaryQuery(periodStart),
            CancellationToken.None);

        result.TotalUsedRequests.Should().Be(8);
        result.ActiveQuotaUsers.Should().Be(2);
        result.RequestsByCurrentTier.Should().Contain(p => p.Label == PlanTier.ProIndividual.ToString() && p.Value == 3);
        result.RequestsByCurrentTier.Should().Contain(p => p.Label == PlanTier.ProCoach.ToString() && p.Value == 5);
        result.RequestsByFeature.Should().Contain(p => p.Label == "food-macro" && p.Value == 4);
        result.RequestsByFeature.Should().Contain(p => p.Label == "coach-client-ai-brief" && p.Value == 4);
        result.TopUsers.Should().HaveCount(2);
        result.TopUsers[0].UserId.Should().Be(coachUserId);
    }

    private async Task<Guid> SeedUserAsync(string email = "user@test.com")
        => await SeedUserAsync(Guid.NewGuid(), email);

    private async Task<Guid> SeedUserAsync(Guid userId, string email)
    {
        await using var ctx = CreateContext();
        ctx.ApplicationUsers.Add(new ApplicationUser
        {
            Id = userId, Email = email, UserName = email,
            FirstName = "Test", LastName = "User"
        });
        await ctx.SaveChangesAsync();
        return userId;
    }

    private async Task SeedPlanAsync(Guid ownerId, bool isActive = false)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        ctx.Plans.Add(new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(27),
            IsActive = isActive
        });
        await ctx.SaveChangesAsync();
    }
}
