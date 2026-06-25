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
    private GetAdminDashboardHandler CreateDashboardHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminUsersHandler CreateUsersHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminPlansHandler CreatePlansHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminUserDetailHandler CreateUserDetailHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminSubscriptionsHandler CreateSubscriptionsHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminAiUsageSummaryHandler CreateAiUsageSummaryHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminInsightsHandler CreateInsightsHandler(ApplicationDbContext ctx) => new(ctx);
    private GetAdminMarketingHandler CreateMarketingHandler(ApplicationDbContext ctx) => new(ctx);
    private GetReportSummaryHandler CreateReportSummaryHandler(ApplicationDbContext ctx) => new(ctx);

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

    [Fact]
    public async Task GetAdminInsights_WithAggregateData_ReturnsTotalsAndTrends()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = await SeedUserAsync("insights@test.com");
        var reportedUserId = await SeedUserAsync("reported@test.com");

        await using var seed = CreateContext();
        var subscription = new UserSubscription
        {
            UserId = userId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        seed.UserSubscriptions.Add(subscription);

        seed.PaymentOrders.Add(new PaymentOrder
        {
            UserId = userId,
            Subscription = subscription,
            RequestedTier = PlanTier.ProIndividual,
            TransferCode = "XENOH1234567890ABCDEF",
            Amount = 149_000m,
            DurationMonths = 1,
            Status = PaymentStatus.Completed,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PaidAt = DateTime.UtcNow
        });

        var plan = new Plan
        {
            Name = "Insights Plan",
            OwnerId = userId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(6),
            WeeklyWorkouts =
            [
                new WeeklyWorkout
                {
                    WeekNumber = 1,
                    Name = "Week 1",
                    StartDate = today,
                    EndDate = today.AddDays(6),
                    DailyWorkouts =
                    [
                        new DailyWorkout
                        {
                            Date = today,
                            DayOfWeek = today.DayOfWeek,
                            Exercises =
                            [
                                new Exercise
                                {
                                    Name = "Squat",
                                    PrimaryMuscleGroup = MuscleGroup.Quads,
                                    ExerciseTemplateId = Guid.NewGuid(),
                                    PlannedSets = 1,
                                    PlannedReps = 5,
                                    Sets =
                                    [
                                        new ExerciseSet
                                        {
                                            SetNumber = 1,
                                            PlannedReps = 5,
                                            ActualReps = 5,
                                            ActualWeight = 100,
                                            IsCompleted = true
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        seed.Plans.Add(plan);

        seed.UserReports.Add(new UserReport
        {
            ReporterId = userId,
            ReportedUserId = reportedUserId,
            Reason = ReportReason.Spam,
            Details = "Spam report"
        });
        seed.AiFeatureUsages.Add(new AiFeatureUsage
        {
            UserId = userId,
            PeriodStart = new DateOnly(today.Year, today.Month, 1),
            Feature = "food-macro",
            UsedRequests = 4
        });
        var share = new TrainingDayShare
        {
            UserId = userId,
            SourceDailyWorkoutId = Guid.NewGuid(),
            WorkoutDate = today,
            DayOfWeek = today.DayOfWeek,
            DayStatus = DayStatus.Normal,
            ExerciseCount = 1,
            CompletedSets = 1,
            TotalVolume = 500
        };
        seed.TrainingDayShares.Add(share);
        seed.TrainingDayShareLoves.Add(new TrainingDayShareLove
        {
            TrainingDayShare = share,
            UserId = reportedUserId
        });
        seed.Friendships.Add(new Friendship
        {
            UserAId = userId,
            UserBId = reportedUserId,
            RequesterId = userId,
            AddresseeId = reportedUserId,
            Status = FriendshipStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateInsightsHandler(ctx).Handle(
            new GetAdminInsightsQuery(today.AddDays(-7), today, AdminInsightGranularity.Day),
            CancellationToken.None);

        result.Totals.NewUsers.Should().BeGreaterThanOrEqualTo(2);
        result.Totals.ActiveUsers.Should().Be(1);
        result.Totals.ActivePaidSubscriptions.Should().Be(1);
        result.Totals.Revenue.Should().Be(149_000m);
        result.Totals.PlansCreated.Should().Be(1);
        result.Totals.CompletedWorkoutDays.Should().Be(1);
        result.Totals.ReportsCreated.Should().Be(1);
        result.Totals.AiRequests.Should().Be(4);
        result.Totals.CommunityShares.Should().Be(1);
        result.Totals.CommunityLoves.Should().Be(1);
        result.Totals.AcceptedFriendships.Should().Be(1);
        result.Revenue.Sum(p => p.Value).Should().Be(149_000m);
    }

    [Fact]
    public async Task GetAdminInsights_WhenDailyRangeExceedsNinetyDays_Throws()
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var act = () => CreateInsightsHandler(ctx).Handle(
            new GetAdminInsightsQuery(today.AddDays(-91), today, AdminInsightGranularity.Day),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Daily admin insights are limited to 90 days.");
    }

    [Fact]
    public async Task GetAdminMarketing_WithActivityEvents_ReturnsTrafficUsageAndFlow()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = await SeedUserAsync("marketing@test.com");

        await using var seed = CreateContext();
        seed.WebsiteActivityEvents.AddRange(
            new WebsiteActivityEvent
            {
                UserId = userId,
                EventType = WebsiteActivityEventType.PageView,
                SessionId = "s1",
                Path = "/",
                UtmSource = "google",
                UtmCampaign = "summer",
                Referrer = "https://google.com",
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-10)
            },
            new WebsiteActivityEvent
            {
                UserId = userId,
                EventType = WebsiteActivityEventType.PageView,
                SessionId = "s1",
                Path = "/pricing",
                PreviousPath = "/",
                UtmSource = "google",
                UtmCampaign = "summer",
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-8)
            },
            new WebsiteActivityEvent
            {
                UserId = userId,
                EventType = WebsiteActivityEventType.Register,
                SessionId = "s1",
                Path = "/register",
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-7)
            },
            new WebsiteActivityEvent
            {
                UserId = userId,
                EventType = WebsiteActivityEventType.Login,
                SessionId = "s1",
                Path = "/login",
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-6)
            },
            new WebsiteActivityEvent
            {
                UserId = userId,
                EventType = WebsiteActivityEventType.SessionUsage,
                SessionId = "s1",
                Path = "/dashboard",
                DurationSeconds = 45,
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });
        seed.WebsiteBugReports.Add(new WebsiteBugReport
        {
            UserId = userId,
            Title = "Broken button",
            Description = "Button does not work",
            Severity = WebsiteBugReportSeverity.High
        });
        await seed.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await CreateMarketingHandler(ctx).Handle(
            new GetAdminMarketingQuery(today.AddDays(-1), today, AdminInsightGranularity.Day),
            CancellationToken.None);

        result.Totals.PageViews.Should().Be(2);
        result.Totals.UniqueSessions.Should().Be(1);
        result.Totals.KnownUsers.Should().Be(1);
        result.Totals.Registrations.Should().Be(1);
        result.Totals.Logins.Should().Be(1);
        result.Totals.TotalUsageSeconds.Should().Be(45);
        result.Totals.BugReportsOpen.Should().Be(1);
        result.TopSources.Should().Contain(p => p.Label == "google" && p.Value == 2);
        result.TopCampaigns.Should().Contain(p => p.Label == "summer" && p.Value == 2);
        result.TopFlows.Should().Contain(f => f.FromPath == "/" && f.ToPath == "/pricing" && f.Count == 1);
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
        var proUserId = Guid.NewGuid();
        await SeedUserAsync(freeUserId, "free@test.com");
        await SeedUserAsync(proUserId, "pro@test.com");

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
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u2, Tier = PlanTier.ProCoach, ExpiresAt = DateTime.UtcNow.AddDays(60) });
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
        seed.UserSubscriptions.Add(new UserSubscription { UserId = u2, Tier = PlanTier.ProCoach, ExpiresAt = DateTime.UtcNow.AddDays(60) });
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
            Id = userId,
            Email = email,
            UserName = email,
            FirstName = "Test",
            LastName = "User"
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
