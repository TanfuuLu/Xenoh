using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using static Xenoh.Application.Features.Admin.AdminQueryHelpers;

namespace Xenoh.Application.Features.Admin;

public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardResponse>;
public sealed record GetReportSummaryQuery : IRequest<AdminReportSummaryResponse>;
public sealed record GetAdminUsersQuery(string? Search, string? Role, PlanTier? Tier, bool? Suspended) : IRequest<List<AdminUserListItemResponse>>;
public sealed record GetAdminUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailResponse>;
public sealed record GetAdminPlansQuery(PlanType? PlanType, Guid? OwnerId, Guid? CoachId, DateOnly? From, DateOnly? To, bool? IsActive) : IRequest<List<AdminPlanListItemResponse>>;
public sealed record GetAdminPlanAnalyticsQuery(Guid PlanId) : IRequest<AdminPlanAnalyticsResponse>;
public sealed record GetAdminPaymentsQuery(PaymentStatus? Status, PlanTier? Tier, DateTime? From, DateTime? To) : IRequest<List<AdminPaymentOrderResponse>>;
public sealed record GetAdminPaymentsSummaryQuery : IRequest<AdminPaymentSummaryResponse>;
public sealed record GetAdminSubscriptionsQuery(PlanTier? Tier, bool? Active) : IRequest<List<AdminSubscriptionResponse>>;
public sealed record GetAdminAiUsageSummaryQuery(DateOnly? PeriodStart) : IRequest<AdminAiUsageSummaryResponse>;
public sealed record GetAdminInsightsQuery(DateOnly? From, DateOnly? To, AdminInsightGranularity? Granularity) : IRequest<AdminInsightsResponse>;
public sealed record AdjustAdminUserSubscriptionCommand(Guid UserId, PlanTier Tier, int? DurationMonths, string Reason) : IRequest<AdminSubscriptionAdjustmentResponse>;
public sealed record GetAdminAuditLogsQuery(int Limit = 100) : IRequest<List<AdminAuditLogResponse>>;
public sealed record GetAdminMarketingQuery(DateOnly? From, DateOnly? To, AdminInsightGranularity? Granularity) : IRequest<AdminMarketingResponse>;

public sealed class GetAdminInsightsHandler(IApplicationDbContext db, IApplicationCache? cache = null)
    : IRequestHandler<GetAdminInsightsQuery, AdminInsightsResponse>
{
    public ValueTask<AdminInsightsResponse> Handle(GetAdminInsightsQuery request, CancellationToken ct) =>
        new(cache is null
            ? BuildAsync(request, ct)
            : cache.GetOrCreateAsync(CacheTags.Admin,
                $"insights:{request.From}:{request.To}:{request.Granularity}", TimeSpan.FromMinutes(1),
                token => BuildAsync(request, token), ct));

    private async Task<AdminInsightsResponse> BuildAsync(GetAdminInsightsQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var to = request.To ?? today;
        var granularity = request.Granularity ?? AdminInsightGranularity.Month;

        if (from > to)
            throw new InvalidOperationException("Insight start date must be before or equal to end date.");

        if (granularity == AdminInsightGranularity.Day && to.DayNumber - from.DayNumber > 90)
            throw new InvalidOperationException("Daily admin insights are limited to 90 days.");

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        var buckets = BuildBuckets(from, to, granularity);

        var userRegistrations = await db.ApplicationUsers.AsNoTracking()
            .Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toExclusiveUtc)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        var paidSubscriptions = await db.UserSubscriptions.AsNoTracking()
            .Where(s => s.Tier != PlanTier.Free && s.ExpiresAt.HasValue)
            .Select(s => new SubscriptionInsightRow(s.CreatedAt, s.ExpiresAt!.Value))
            .ToListAsync(ct);

        var revenueRows = await db.PaymentOrders.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt.HasValue && p.PaidAt.Value >= fromUtc && p.PaidAt.Value < toExclusiveUtc)
            .Select(p => new AmountInsightRow(p.PaidAt!.Value, p.Amount))
            .ToListAsync(ct);

        var planRows = await db.Plans.AsNoTracking()
            .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toExclusiveUtc)
            .Select(p => new UserDateTimeInsightRow(p.OwnerId, p.CreatedAt))
            .ToListAsync(ct);

        var completedWorkoutDays = await db.DailyWorkouts.AsNoTracking()
            .Where(d =>
                d.Date >= from &&
                d.Date <= to &&
                d.Exercises.Any() &&
                !d.Exercises.Any(e => !e.Sets.Any() || e.Sets.Any(s => !s.IsCompleted)))
            .Select(d => d.Date)
            .ToListAsync(ct);

        var reports = await db.UserReports.AsNoTracking()
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toExclusiveUtc)
            .Select(r => r.CreatedAt)
            .ToListAsync(ct);

        var aiFrom = new DateOnly(from.Year, from.Month, 1);
        var aiRowsRaw = await db.AiFeatureUsages.AsNoTracking()
            .Where(u => u.PeriodStart >= aiFrom && u.PeriodStart <= to)
            .Select(u => new UserDateOnlyAmountInsightRow(u.UserId, u.PeriodStart, u.UsedRequests))
            .ToListAsync(ct);
        var aiRows = aiRowsRaw
            .Select(row => row with { Date = row.Date < from ? from : row.Date })
            .ToList();

        var workoutUsers = await db.WorkoutHistories.AsNoTracking()
            .Where(w => w.Date >= from && w.Date <= to)
            .Select(w => new UserDateOnlyInsightRow(w.UserId, w.Date))
            .ToListAsync(ct);

        var foodUsers = await db.FoodLogs.AsNoTracking()
            .Where(f => f.Date >= from && f.Date <= to)
            .Select(f => new UserDateOnlyInsightRow(f.UserId, f.Date))
            .ToListAsync(ct);

        var shareRows = await db.TrainingDayShares.AsNoTracking()
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toExclusiveUtc)
            .Select(s => new UserDateTimeInsightRow(s.UserId, s.CreatedAt))
            .ToListAsync(ct);

        var loveRows = await db.TrainingDayShareLoves.AsNoTracking()
            .Where(l => l.CreatedAt >= fromUtc && l.CreatedAt < toExclusiveUtc)
            .Select(l => l.CreatedAt)
            .ToListAsync(ct);

        var friendshipRows = await db.Friendships.AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted && f.RespondedAt.HasValue && f.RespondedAt.Value >= fromUtc && f.RespondedAt.Value < toExclusiveUtc)
            .Select(f => f.RespondedAt!.Value)
            .ToListAsync(ct);

        var activeUserIds = workoutUsers.Select(r => r.UserId)
            .Concat(foodUsers.Select(r => r.UserId))
            .Concat(planRows.Select(r => r.UserId))
            .Concat(aiRows.Select(r => r.UserId))
            .Concat(shareRows.Select(r => r.UserId))
            .Distinct()
            .Count();

        var totals = new AdminInsightTotalsResponse(
            TotalUsers: await db.ApplicationUsers.AsNoTracking().CountAsync(ct),
            NewUsers: userRegistrations.Count,
            ActiveUsers: activeUserIds,
            ActivePaidSubscriptions: paidSubscriptions.Count(s => s.ExpiresAt > now),
            Revenue: revenueRows.Sum(r => r.Amount),
            PlansCreated: planRows.Count,
            CompletedWorkoutDays: completedWorkoutDays.Count,
            ReportsCreated: reports.Count,
            AiRequests: aiRows.Sum(r => r.Value),
            CommunityShares: shareRows.Count,
            CommunityLoves: loveRows.Count,
            AcceptedFriendships: friendshipRows.Count);

        return new AdminInsightsResponse(
            from,
            to,
            granularity,
            totals,
            CountDateTimes(buckets, userRegistrations),
            CountActiveUsers(buckets, workoutUsers, foodUsers, planRows, aiRows, shareRows),
            CountActiveSubscriptions(buckets, paidSubscriptions),
            SumAmounts(buckets, revenueRows),
            CountDateTimes(buckets, planRows.Select(p => p.Date).ToList()),
            CountDateOnlys(buckets, completedWorkoutDays),
            CountDateTimes(buckets, reports),
            SumDateOnlyAmounts(buckets, aiRows),
            SumCommunityActivity(buckets, shareRows, loveRows, friendshipRows));
    }

    private static List<InsightBucket> BuildBuckets(DateOnly from, DateOnly to, AdminInsightGranularity granularity)
    {
        if (granularity == AdminInsightGranularity.Day)
        {
            return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
                .Select(i =>
                {
                    var day = from.AddDays(i);
                    return new InsightBucket(day, day, day.ToString("yyyy-MM-dd"));
                })
                .ToList();
        }

        var start = new DateOnly(from.Year, from.Month, 1);
        var buckets = new List<InsightBucket>();
        for (var month = start; month <= to; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            buckets.Add(new InsightBucket(
                month < from ? from : month,
                monthEnd > to ? to : monthEnd,
                month.ToString("MMM yyyy")));
        }

        return buckets;
    }

    private static List<AdminMetricPointResponse> CountDateTimes(List<InsightBucket> buckets, List<DateTime> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Count(row => bucket.Contains(row))))
        .ToList();

    private static List<AdminMetricPointResponse> CountDateOnlys(List<InsightBucket> buckets, List<DateOnly> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Count(bucket.Contains)))
        .ToList();

    private static List<AdminMetricPointResponse> SumAmounts(List<InsightBucket> buckets, List<AmountInsightRow> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Where(row => bucket.Contains(row.Date)).Sum(row => row.Amount)))
        .ToList();

    private static List<AdminMetricPointResponse> SumDateOnlyAmounts(List<InsightBucket> buckets, List<UserDateOnlyAmountInsightRow> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Where(row => bucket.Contains(row.Date)).Sum(row => row.Value)))
        .ToList();

    private static List<AdminMetricPointResponse> CountActiveSubscriptions(List<InsightBucket> buckets, List<SubscriptionInsightRow> rows) =>
        buckets.Select(bucket =>
        {
            var endExclusive = bucket.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return new AdminMetricPointResponse(
                bucket.Label,
                rows.Count(row => row.CreatedAt < endExclusive && row.ExpiresAt >= endExclusive));
        })
        .ToList();

    private static List<AdminMetricPointResponse> CountActiveUsers(
        List<InsightBucket> buckets,
        List<UserDateOnlyInsightRow> workouts,
        List<UserDateOnlyInsightRow> foods,
        List<UserDateTimeInsightRow> plans,
        List<UserDateOnlyAmountInsightRow> ai,
        List<UserDateTimeInsightRow> shares) =>
        buckets.Select(bucket =>
        {
            var count = workouts.Where(row => bucket.Contains(row.Date)).Select(row => row.UserId)
                .Concat(foods.Where(row => bucket.Contains(row.Date)).Select(row => row.UserId))
                .Concat(plans.Where(row => bucket.Contains(row.Date)).Select(row => row.UserId))
                .Concat(ai.Where(row => bucket.Contains(row.Date)).Select(row => row.UserId))
                .Concat(shares.Where(row => bucket.Contains(row.Date)).Select(row => row.UserId))
                .Distinct()
                .Count();

            return new AdminMetricPointResponse(bucket.Label, count);
        })
        .ToList();

    private static List<AdminMetricPointResponse> SumCommunityActivity(
        List<InsightBucket> buckets,
        List<UserDateTimeInsightRow> shares,
        List<DateTime> loves,
        List<DateTime> friendships) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            shares.Count(row => bucket.Contains(row.Date)) +
            loves.Count(bucket.Contains) +
            friendships.Count(bucket.Contains)))
        .ToList();

    private sealed record InsightBucket(DateOnly From, DateOnly To, string Label)
    {
        public bool Contains(DateOnly date) => date >= From && date <= To;
        public bool Contains(DateTime dateTime) => Contains(DateOnly.FromDateTime(dateTime));
    }

    private sealed record SubscriptionInsightRow(DateTime CreatedAt, DateTime ExpiresAt);
    private sealed record AmountInsightRow(DateTime Date, decimal Amount);
    private sealed record UserDateTimeInsightRow(Guid UserId, DateTime Date);
    private sealed record UserDateOnlyInsightRow(Guid UserId, DateOnly Date);
    private sealed record UserDateOnlyAmountInsightRow(Guid UserId, DateOnly Date, int Value);
}

public sealed class AdjustAdminUserSubscriptionHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<AdjustAdminUserSubscriptionCommand, AdminSubscriptionAdjustmentResponse>
{
    private static readonly int[] AllowedDurations = [1, 3, 6, 12];

    public async ValueTask<AdminSubscriptionAdjustmentResponse> Handle(AdjustAdminUserSubscriptionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Adjustment reason is required.");

        if (request.Tier != PlanTier.Free && (!request.DurationMonths.HasValue || !AllowedDurations.Contains(request.DurationMonths.Value)))
            throw new InvalidOperationException("Paid subscription adjustments require a duration of 1, 3, 6, or 12 months.");

        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var now = DateTime.UtcNow;
        var subscription = await db.UserSubscriptions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, ct);

        if (subscription is null)
        {
            subscription = new UserSubscription
            {
                UserId = request.UserId,
                Tier = PlanTier.Free
            };
            db.UserSubscriptions.Add(subscription);
        }

        var before = DescribeSubscription(subscription, now);

        if (request.Tier == PlanTier.Free)
        {
            subscription.Tier = PlanTier.Free;
            subscription.ExpiresAt = null;
        }
        else
        {
            var sameActiveTier = subscription.Tier == request.Tier &&
                subscription.ExpiresAt.HasValue &&
                subscription.ExpiresAt.Value > now;
            var baseDate = sameActiveTier ? subscription.ExpiresAt!.Value : now;
            subscription.Tier = request.Tier;
            subscription.ExpiresAt = baseDate.AddMonths(request.DurationMonths!.Value);
        }

        subscription.UpdatedAt = now;
        await SyncCoachRoleAsync(user, request.Tier, ct);

        var after = DescribeSubscription(subscription, now);
        var audit = AdminAudit.Add(
            db,
            currentUser.UserId,
            AdminAudit.AdjustSubscription,
            nameof(UserSubscription),
            subscription.Id,
            user.Id,
            request.Reason,
            before,
            after);

        await db.SaveChangesAsync(ct);

        return new AdminSubscriptionAdjustmentResponse(
            user.Id,
            FullName(user),
            user.Email ?? string.Empty,
            subscription.Tier,
            IsSubscriptionActive(subscription, now),
            subscription.ExpiresAt,
            audit.Id);
    }

    private async Task SyncCoachRoleAsync(ApplicationUser user, PlanTier tier, CancellationToken ct)
    {
        if (tier == PlanTier.ProCoach)
        {
            if (!await userManager.IsInRoleAsync(user, UserRole.Coach))
            {
                var result = await userManager.AddToRoleAsync(user, UserRole.Coach);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Could not grant Coach role: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            return;
        }

        if (await userManager.IsInRoleAsync(user, UserRole.Coach))
        {
            var result = await userManager.RemoveFromRoleAsync(user, UserRole.Coach);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not remove Coach role: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }

    private static string DescribeSubscription(UserSubscription subscription, DateTime now)
    {
        var active = IsSubscriptionActive(subscription, now) ? "active" : "inactive";
        var expiry = subscription.ExpiresAt?.ToString("O") ?? "none";
        return $"Tier={subscription.Tier}; Active={active}; ExpiresAt={expiry}";
    }
}

public sealed class GetAdminAuditLogsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminAuditLogsQuery, List<AdminAuditLogResponse>>
{
    public async ValueTask<List<AdminAuditLogResponse>> Handle(GetAdminAuditLogsQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        return await db.AdminAuditLogs
            .AsNoTracking()
            .Include(log => log.AdminUser)
            .OrderByDescending(log => log.CreatedAt)
            .Take(limit)
            .Select(log => new AdminAuditLogResponse(
                log.Id,
                log.AdminUserId,
                FullName(log.AdminUser),
                log.Action,
                log.TargetType,
                log.TargetId,
                log.TargetUserId,
                log.Reason,
                log.BeforeSummary,
                log.AfterSummary,
                log.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetAdminMarketingHandler(IApplicationDbContext db, IApplicationCache? cache = null)
    : IRequestHandler<GetAdminMarketingQuery, AdminMarketingResponse>
{
    public ValueTask<AdminMarketingResponse> Handle(GetAdminMarketingQuery request, CancellationToken ct) =>
        new(cache is null
            ? BuildAsync(request, ct)
            : cache.GetOrCreateAsync(CacheTags.Admin,
                $"marketing:{request.From}:{request.To}:{request.Granularity}", TimeSpan.FromMinutes(1),
                token => BuildAsync(request, token), ct));

    private async Task<AdminMarketingResponse> BuildAsync(GetAdminMarketingQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var to = request.To ?? today;
        var granularity = request.Granularity ?? AdminInsightGranularity.Month;

        if (from > to)
            throw new InvalidOperationException("Marketing start date must be before or equal to end date.");

        if (granularity == AdminInsightGranularity.Day && to.DayNumber - from.DayNumber > 90)
            throw new InvalidOperationException("Daily admin marketing insights are limited to 90 days.");

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var buckets = BuildMarketingBuckets(from, to, granularity);

        var events = await db.WebsiteActivityEvents
            .AsNoTracking()
            .Where(e => e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc < toExclusiveUtc)
            .Select(e => new MarketingEventRow(
                e.UserId,
                e.EventType,
                e.SessionId,
                e.Path,
                e.PreviousPath,
                e.Referrer,
                e.UtmSource,
                e.UtmCampaign,
                e.DurationSeconds,
                e.OccurredAtUtc))
            .ToListAsync(ct);

        var pageViews = events.Where(e => e.EventType == WebsiteActivityEventType.PageView).ToList();
        var usage = events.Where(e => e.EventType == WebsiteActivityEventType.SessionUsage).ToList();
        var logins = events.Where(e => e.EventType == WebsiteActivityEventType.Login).ToList();
        var registrations = events.Where(e => e.EventType == WebsiteActivityEventType.Register).ToList();
        var usageSeconds = usage.Sum(e => e.DurationSeconds ?? 0);
        var uniqueSessions = events.Select(e => e.SessionId).Where(s => s.Length > 0).Distinct().Count();
        var bugReportsOpen = await db.WebsiteBugReports
            .AsNoTracking()
            .CountAsync(r => r.Status == WebsiteBugReportStatus.Open, ct);

        return new AdminMarketingResponse(
            from,
            to,
            granularity,
            new AdminMarketingTotalsResponse(
                pageViews.Count,
                uniqueSessions,
                events.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value).Distinct().Count(),
                logins.Count,
                registrations.Count,
                usageSeconds,
                uniqueSessions == 0 ? 0m : Math.Round(usageSeconds / (decimal)uniqueSessions, 2),
                bugReportsOpen),
            CountMarketingEvents(buckets, pageViews),
            CountMarketingEvents(buckets, logins),
            CountMarketingEvents(buckets, registrations),
            SumUsageSeconds(buckets, usage),
            TopMetric(pageViews.Select(e => e.UtmSource), 8),
            TopMetric(pageViews.Select(e => e.UtmCampaign), 8),
            TopMetric(pageViews.Select(e => e.Referrer), 8),
            TopMetric(GetEntryPages(pageViews), 8),
            TopFlows(pageViews, 10));
    }

    private static List<MarketingBucket> BuildMarketingBuckets(DateOnly from, DateOnly to, AdminInsightGranularity granularity)
    {
        if (granularity == AdminInsightGranularity.Day)
        {
            return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
                .Select(i =>
                {
                    var day = from.AddDays(i);
                    return new MarketingBucket(day, day, day.ToString("yyyy-MM-dd"));
                })
                .ToList();
        }

        var start = new DateOnly(from.Year, from.Month, 1);
        var buckets = new List<MarketingBucket>();
        for (var month = start; month <= to; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            buckets.Add(new MarketingBucket(
                month < from ? from : month,
                monthEnd > to ? to : monthEnd,
                month.ToString("MMM yyyy")));
        }

        return buckets;
    }

    private static List<AdminMetricPointResponse> CountMarketingEvents(List<MarketingBucket> buckets, List<MarketingEventRow> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Count(row => bucket.Contains(row.OccurredAtUtc))))
        .ToList();

    private static List<AdminMetricPointResponse> SumUsageSeconds(List<MarketingBucket> buckets, List<MarketingEventRow> rows) =>
        buckets.Select(bucket => new AdminMetricPointResponse(
            bucket.Label,
            rows.Where(row => bucket.Contains(row.OccurredAtUtc)).Sum(row => row.DurationSeconds ?? 0)))
        .ToList();

    private static IEnumerable<string?> GetEntryPages(List<MarketingEventRow> pageViews) =>
        pageViews
            .GroupBy(e => e.SessionId)
            .Select(g => g.OrderBy(e => e.OccurredAtUtc).FirstOrDefault()?.Path);

    private static List<AdminMetricPointResponse> TopMetric(IEnumerable<string?> values, int take) =>
        values
            .Select(value => string.IsNullOrWhiteSpace(value) ? "(none)" : value.Trim())
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(g => new AdminMetricPointResponse(g.Key, g.Count()))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Label)
            .Take(take)
            .ToList();

    private static List<AdminMarketingFlowResponse> TopFlows(List<MarketingEventRow> pageViews, int take) =>
        pageViews
            .Where(e => !string.IsNullOrWhiteSpace(e.PreviousPath) && !string.Equals(e.PreviousPath, e.Path, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => new { From = e.PreviousPath!, To = e.Path })
            .Select(g => new AdminMarketingFlowResponse(g.Key.From, g.Key.To, g.Count()))
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.FromPath)
            .ThenBy(f => f.ToPath)
            .Take(take)
            .ToList();

    private sealed record MarketingBucket(DateOnly From, DateOnly To, string Label)
    {
        public bool Contains(DateTime dateTime)
        {
            var date = DateOnly.FromDateTime(dateTime);
            return date >= From && date <= To;
        }
    }

    private sealed record MarketingEventRow(
        Guid? UserId,
        WebsiteActivityEventType EventType,
        string SessionId,
        string Path,
        string? PreviousPath,
        string? Referrer,
        string? UtmSource,
        string? UtmCampaign,
        int? DurationSeconds,
        DateTime OccurredAtUtc);
}

public sealed class GetAdminDashboardHandler(IApplicationDbContext db, IApplicationCache? cache = null)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public ValueTask<AdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken ct) =>
        new(cache is null
            ? BuildAsync(ct)
            : cache.GetOrCreateAsync(CacheTags.Admin, "dashboard", TimeSpan.FromMinutes(1), BuildAsync, ct));

    private async Task<AdminDashboardResponse> BuildAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var activePaidSubscriptionsQuery = db.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.Tier != PlanTier.Free && s.ExpiresAt.HasValue && s.ExpiresAt.Value > now);

        var activeCoaches = await CountUsersInRoleAsync(db, UserRole.Coach, ct);

        var subscriptionDistribution = await db.UserSubscriptions
            .AsNoTracking()
            .GroupBy(s => s.Tier)
            .Select(g => new AdminMetricPointResponse(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        return new AdminDashboardResponse(
            TotalUsers: await db.ApplicationUsers.AsNoTracking().CountAsync(ct),
            NewUsersThisMonth: await db.ApplicationUsers.AsNoTracking().CountAsync(u => u.CreatedAt >= monthStart, ct),
            ActiveCoaches: activeCoaches,
            ActivePaidSubscriptions: await activePaidSubscriptionsQuery.CountAsync(ct),
            PendingReports: await db.UserReports.AsNoTracking().CountAsync(r => r.Status == ReportStatus.Pending, ct),
            CompletedPaymentRevenueThisMonth: await db.PaymentOrders.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt.HasValue && p.PaidAt.Value >= monthStart)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m,
            TotalPlansCreated: await db.Plans.AsNoTracking().CountAsync(ct),
            CompletedWorkoutDays: await CountCompletedWorkoutDaysAsync(db.DailyWorkouts.AsNoTracking(), ct),
            UserRegistrations: await BuildUserRegistrationTrendAsync(db, now, ct),
            Revenue: await BuildRevenueTrendAsync(db, now, ct),
            SubscriptionTierDistribution: subscriptionDistribution,
            PlanCompletionTrend: await BuildPlanCompletionTrendAsync(db, now, ct));
    }

    private static async Task<List<AdminMetricPointResponse>> BuildUserRegistrationTrendAsync(IApplicationDbContext db, DateTime now, CancellationToken ct)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var users = await db.ApplicationUsers.AsNoTracking()
            .Where(u => u.CreatedAt >= start)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        return Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .Select(month => new AdminMetricPointResponse(
                month.ToString("MMM yyyy"),
                users.Count(created => created.Year == month.Year && created.Month == month.Month)))
            .ToList();
    }

    private static async Task<List<AdminMetricPointResponse>> BuildRevenueTrendAsync(IApplicationDbContext db, DateTime now, CancellationToken ct)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var payments = await db.PaymentOrders.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt.HasValue && p.PaidAt.Value >= start)
            .Select(p => new { PaidAt = p.PaidAt!.Value, p.Amount })
            .ToListAsync(ct);

        return Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .Select(month => new AdminMetricPointResponse(
                month.ToString("MMM yyyy"),
                payments.Where(p => p.PaidAt.Year == month.Year && p.PaidAt.Month == month.Month).Sum(p => p.Amount)))
            .ToList();
    }

    private static async Task<List<AdminMetricPointResponse>> BuildPlanCompletionTrendAsync(IApplicationDbContext db, DateTime now, CancellationToken ct)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var plans = await ProjectAdminPlanRowsAsync(
            db.Plans.AsNoTracking().Where(p => p.CreatedAt >= start),
            ct);

        return Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .Select(month =>
            {
                var monthPlans = plans.Where(p => p.CreatedAt.Year == month.Year && p.CreatedAt.Month == month.Month).ToList();
                var average = monthPlans.Count == 0
                    ? 0m
                    : Math.Round(monthPlans.Average(p => p.CompletionPercent), 2);
                return new AdminMetricPointResponse(month.ToString("MMM yyyy"), average);
            })
            .ToList();
    }
}

public sealed class GetReportSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReportSummaryQuery, AdminReportSummaryResponse>
{
    public async ValueTask<AdminReportSummaryResponse> Handle(GetReportSummaryQuery request, CancellationToken ct)
    {
        var counts = await db.UserReports.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new AdminStatusCountResponse(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        return new AdminReportSummaryResponse(await db.UserReports.AsNoTracking().CountAsync(ct), counts);
    }
}

public sealed class GetAdminUsersHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminUsersQuery, List<AdminUserListItemResponse>>
{
    public async ValueTask<List<AdminUserListItemResponse>> Handle(GetAdminUsersQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var usersQuery = db.ApplicationUsers
            .AsNoTracking()
            .Include(u => u.Subscription)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email!.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        if (request.Tier.HasValue)
        {
            usersQuery = request.Tier.Value == PlanTier.Free
                ? usersQuery.Where(u => u.Subscription == null || u.Subscription.Tier == PlanTier.Free)
                : usersQuery.Where(u => u.Subscription != null && u.Subscription.Tier == request.Tier.Value);
        }

        if (request.Suspended.HasValue)
        {
            usersQuery = request.Suspended.Value
                ? usersQuery.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > now)
                : usersQuery.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= now);
        }

        var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
        var userIds = users.Select(u => u.Id).ToList();
        var roleMap = await GetRoleMapAsync(db, userIds, ct);
        var planCounts = await CountByUserAsync(db.Plans.AsNoTracking().Where(p => userIds.Contains(p.OwnerId)), p => p.OwnerId, ct);
        var workoutCounts = await CountByUserAsync(db.WorkoutHistories.AsNoTracking().Where(w => userIds.Contains(w.UserId)), w => w.UserId, ct);
        var reportsMade = await CountByUserAsync(db.UserReports.AsNoTracking().Where(r => userIds.Contains(r.ReporterId)), r => r.ReporterId, ct);
        var reportsReceived = await CountByUserAsync(db.UserReports.AsNoTracking().Where(r => userIds.Contains(r.ReportedUserId)), r => r.ReportedUserId, ct);

        var result = users
            .Select(user => ToUserListItem(user, roleMap, planCounts, workoutCounts, reportsMade, reportsReceived, now))
            .ToList();

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            result = result
                .Where(u => u.Roles.Any(role => role.Equals(request.Role, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return result;
    }
}

public sealed class GetAdminUserDetailHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailResponse>
{
    public async ValueTask<AdminUserDetailResponse> Handle(GetAdminUserDetailQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var user = await db.ApplicationUsers
            .AsNoTracking()
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        var roles = await GetUserRolesAsync(db, request.UserId, ct);
        var planCount = await db.Plans.AsNoTracking().CountAsync(p => p.OwnerId == request.UserId, ct);
        var workoutCount = await db.WorkoutHistories.AsNoTracking().CountAsync(w => w.UserId == request.UserId, ct);
        var reportsMade = await db.UserReports.AsNoTracking().CountAsync(r => r.ReporterId == request.UserId, ct);
        var reportsReceived = await db.UserReports.AsNoTracking().CountAsync(r => r.ReportedUserId == request.UserId, ct);

        var coachRelationshipEntity = await db.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Coach)
            .Where(r => r.ClientId == request.UserId && r.Status != RelationshipStatus.Ended)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var coachRelationship = coachRelationshipEntity is null
            ? null
            : new AdminUserRelationshipResponse(
                coachRelationshipEntity.Id,
                coachRelationshipEntity.CoachId,
                FullName(coachRelationshipEntity.Coach),
                coachRelationshipEntity.Coach.Email ?? string.Empty,
                coachRelationshipEntity.Status);

        var clientRelationshipEntities = await db.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Client)
            .Where(r => r.CoachId == request.UserId && r.Status != RelationshipStatus.Ended)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var clientRelationships = clientRelationshipEntities
            .Select(r => new AdminUserRelationshipResponse(r.Id, r.ClientId, FullName(r.Client), r.Client.Email ?? string.Empty, r.Status))
            .ToList();

        return new AdminUserDetailResponse(
            user.Id,
            user.Email ?? string.Empty,
            FullName(user),
            roles,
            user.Subscription?.Tier ?? PlanTier.Free,
            user.Subscription?.ExpiresAt,
            IsSubscriptionActive(user.Subscription, now),
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > now,
            user.CreatedAt,
            user.Height,
            user.Gender,
            user.DateOfBirth,
            user.Bio,
            user.AvatarUrl,
            planCount,
            workoutCount,
            reportsMade,
            reportsReceived,
            coachRelationship,
            clientRelationships);
    }
}

public sealed class GetAdminPlansHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPlansQuery, List<AdminPlanListItemResponse>>
{
    public async ValueTask<List<AdminPlanListItemResponse>> Handle(GetAdminPlansQuery request, CancellationToken ct)
    {
        var query = db.Plans.AsNoTracking().AsQueryable();

        if (request.PlanType.HasValue)
        {
            query = query.Where(p => p.PlanType == request.PlanType.Value);
        }

        if (request.OwnerId.HasValue)
        {
            query = query.Where(p => p.OwnerId == request.OwnerId.Value);
        }

        if (request.CoachId.HasValue)
        {
            query = query.Where(p => p.CreatedByCoachId == request.CoachId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(p => p.StartDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(p => p.EndDate <= request.To.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var plans = await ProjectAdminPlanRowsAsync(query.OrderByDescending(p => p.CreatedAt), ct);
        return plans.Select(ToAdminPlanListItem).ToList();
    }
}

public sealed class GetAdminPlanAnalyticsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPlanAnalyticsQuery, AdminPlanAnalyticsResponse>
{
    public async ValueTask<AdminPlanAnalyticsResponse> Handle(GetAdminPlanAnalyticsQuery request, CancellationToken ct)
    {
        var plan = await ProjectAdminPlanRowsAsync(db.Plans.AsNoTracking().Where(p => p.Id == request.PlanId), ct);
        var target = plan.FirstOrDefault() ?? throw new InvalidOperationException("Plan not found.");

        return new AdminPlanAnalyticsResponse(
            target.Id,
            target.Name,
            target.OwnerId,
            FullName(target.OwnerFirstName, target.OwnerLastName, target.OwnerEmail),
            target.CreatedByCoachId,
            target.CreatedByCoachId is null ? null : FullName(target.CoachFirstName ?? string.Empty, target.CoachLastName ?? string.Empty, target.CoachEmail),
            target.TotalWeeks,
            target.TotalDays,
            target.CompletedDays,
            target.CompletionPercent,
            target.TotalExercises,
            target.TotalCompletedSets,
            target.TotalVolume);
    }
}

public sealed class GetAdminPaymentsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPaymentsQuery, List<AdminPaymentOrderResponse>>
{
    public async ValueTask<List<AdminPaymentOrderResponse>> Handle(GetAdminPaymentsQuery request, CancellationToken ct)
    {
        var query = db.PaymentOrders
            .AsNoTracking()
            .Include(p => p.User)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.Tier.HasValue)
        {
            query = query.Where(p => p.RequestedTier == request.Tier.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= request.To.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return payments
            .Select(p => new AdminPaymentOrderResponse(
                p.Id,
                p.UserId,
                FullName(p.User),
                p.User.Email ?? string.Empty,
                p.RequestedTier,
                p.DurationMonths,
                p.Amount,
                p.Status,
                p.TransferCode,
                p.SePayTransactionId,
                p.SePayReferenceCode,
                p.CreatedAt,
                p.ExpiresAt,
                p.PaidAt))
            .ToList();
    }
}

public sealed class GetAdminPaymentsSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPaymentsSummaryQuery, AdminPaymentSummaryResponse>
{
    public async ValueTask<AdminPaymentSummaryResponse> Handle(GetAdminPaymentsSummaryQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new AdminPaymentSummaryResponse(
            TotalRevenue: await db.PaymentOrders.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m,
            RevenueThisMonth: await db.PaymentOrders.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt.HasValue && p.PaidAt.Value >= monthStart)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m,
            PendingAmount: await db.PaymentOrders.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m,
            CompletedOrders: await db.PaymentOrders.AsNoTracking().CountAsync(p => p.Status == PaymentStatus.Completed, ct),
            ActivePaidSubscriptions: await db.UserSubscriptions.AsNoTracking()
                .CountAsync(s => s.Tier != PlanTier.Free && s.ExpiresAt.HasValue && s.ExpiresAt.Value > now, ct),
            ProIndividualSubscriptions: await db.UserSubscriptions.AsNoTracking()
                .CountAsync(s => s.Tier == PlanTier.ProIndividual && s.ExpiresAt.HasValue && s.ExpiresAt.Value > now, ct),
            ProCoachSubscriptions: await db.UserSubscriptions.AsNoTracking()
                .CountAsync(s => s.Tier == PlanTier.ProCoach && s.ExpiresAt.HasValue && s.ExpiresAt.Value > now, ct));
    }
}

public sealed class GetAdminSubscriptionsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminSubscriptionsQuery, List<AdminSubscriptionResponse>>
{
    public async ValueTask<List<AdminSubscriptionResponse>> Handle(GetAdminSubscriptionsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var query = db.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.User)
            .AsQueryable();

        if (request.Tier.HasValue)
        {
            query = query.Where(s => s.Tier == request.Tier.Value);
        }

        var subscriptions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return subscriptions
            .Where(s => !request.Active.HasValue || IsSubscriptionActive(s, now) == request.Active.Value)
            .Select(s => new AdminSubscriptionResponse(
                s.UserId,
                FullName(s.User),
                s.User.Email ?? string.Empty,
                s.Tier,
                IsSubscriptionActive(s, now),
                s.ExpiresAt,
                s.CreatedAt))
            .ToList();
    }
}

public sealed class GetAdminAiUsageSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminAiUsageSummaryQuery, AdminAiUsageSummaryResponse>
{
    public async ValueTask<AdminAiUsageSummaryResponse> Handle(GetAdminAiUsageSummaryQuery request, CancellationToken ct)
    {
        var periodStart = request.PeriodStart ?? CurrentPeriodStart();

        var quotas = await db.AiUsageQuotas
            .AsNoTracking()
            .Include(q => q.User)
            .Where(q => q.PeriodStart == periodStart)
            .ToListAsync(ct);

        var userIds = quotas.Select(q => q.UserId).Distinct().ToList();
        var tiers = await db.UserSubscriptions
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.Tier })
            .ToDictionaryAsync(s => s.UserId, s => s.Tier, ct);

        var requestsByTier = quotas
            .GroupBy(q => tiers.GetValueOrDefault(q.UserId, PlanTier.Free))
            .Select(g => new AdminMetricPointResponse(g.Key.ToString(), g.Sum(q => q.UsedRequests)))
            .OrderBy(p => p.Label)
            .ToList();

        var featureRows = await db.AiFeatureUsages
            .AsNoTracking()
            .Where(u => u.PeriodStart == periodStart)
            .ToListAsync(ct);

        var requestsByFeature = featureRows
            .GroupBy(u => u.Feature)
            .Select(g => new AdminMetricPointResponse(g.Key, g.Sum(u => u.UsedRequests)))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Label)
            .ToList();

        var topUsers = quotas
            .OrderByDescending(q => q.UsedRequests)
            .ThenBy(q => q.User.Email)
            .Take(10)
            .Select(q => new AdminAiUsageTopUserResponse(
                q.UserId,
                FullName(q.User),
                q.User.Email ?? string.Empty,
                tiers.GetValueOrDefault(q.UserId, PlanTier.Free),
                q.UsedRequests,
                q.LastConsumedAt))
            .ToList();

        return new AdminAiUsageSummaryResponse(
            periodStart,
            quotas.Sum(q => q.UsedRequests),
            quotas.Count(q => q.UsedRequests > 0),
            requestsByTier,
            requestsByFeature,
            topUsers);
    }

    private static DateOnly CurrentPeriodStart()
    {
        var now = DateTime.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }
}

internal static class AdminQueryHelpers
{
    internal static async Task<int> CountUsersInRoleAsync(IApplicationDbContext db, string roleName, CancellationToken ct)
    {
        return await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == roleName
            select userRole.UserId)
            .Distinct()
            .CountAsync(ct);
    }

    internal static async Task<Dictionary<Guid, List<string>>> GetRoleMapAsync(IApplicationDbContext db, List<Guid> userIds, CancellationToken ct)
    {
        var rows = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name ?? string.Empty })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoleName).Where(r => r.Length > 0).Order().ToList());
    }

    internal static async Task<List<string>> GetUserRolesAsync(IApplicationDbContext db, Guid userId, CancellationToken ct)
    {
        var roleMap = await GetRoleMapAsync(db, [userId], ct);
        return roleMap.TryGetValue(userId, out var roles) ? roles : [];
    }

    internal static async Task<Dictionary<Guid, int>> CountByUserAsync<TEntity>(
        IQueryable<TEntity> query,
        System.Linq.Expressions.Expression<Func<TEntity, Guid>> userIdSelector,
        CancellationToken ct)
    {
        return await query
            .GroupBy(userIdSelector)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);
    }

    internal static AdminUserListItemResponse ToUserListItem(
        ApplicationUser user,
        Dictionary<Guid, List<string>> roleMap,
        Dictionary<Guid, int> planCounts,
        Dictionary<Guid, int> workoutCounts,
        Dictionary<Guid, int> reportsMade,
        Dictionary<Guid, int> reportsReceived,
        DateTime now)
    {
        return new AdminUserListItemResponse(
            user.Id,
            user.Email ?? string.Empty,
            FullName(user),
            roleMap.TryGetValue(user.Id, out var roles) ? roles : [],
            user.Subscription?.Tier ?? PlanTier.Free,
            IsSubscriptionActive(user.Subscription, now),
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > now,
            user.CreatedAt,
            planCounts.GetValueOrDefault(user.Id),
            workoutCounts.GetValueOrDefault(user.Id),
            reportsMade.GetValueOrDefault(user.Id),
            reportsReceived.GetValueOrDefault(user.Id));
    }

    internal static async Task<List<AdminPlanRow>> ProjectAdminPlanRowsAsync(IQueryable<Plan> query, CancellationToken ct)
    {
        return await query
            .Select(p => new AdminPlanRow(
                p.Id,
                p.Name,
                p.PlanType,
                p.OwnerId,
                p.Owner.FirstName,
                p.Owner.LastName,
                p.Owner.Email ?? string.Empty,
                p.CreatedByCoachId,
                p.CreatedByCoach == null ? null : p.CreatedByCoach.FirstName,
                p.CreatedByCoach == null ? null : p.CreatedByCoach.LastName,
                p.CreatedByCoach == null ? null : p.CreatedByCoach.Email,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.CreatedAt,
                p.WeeklyWorkouts.Count,
                p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(),
                p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts)
                    .Count(d =>
                        d.Exercises.Any() &&
                        !d.Exercises.Any(e => !e.Sets.Any() || e.Sets.Any(s => !s.IsCompleted))),
                p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).SelectMany(d => d.Exercises).Count(),
                p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).SelectMany(d => d.Exercises).SelectMany(e => e.Sets)
                    .Count(s => s.IsCompleted),
                p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).SelectMany(d => d.Exercises).SelectMany(e => e.Sets)
                    .Where(s => s.IsCompleted)
                    .Sum(s => (decimal?)((s.ActualReps ?? 0) * (s.ActualWeight ?? 0m))) ?? 0m))
            .ToListAsync(ct);
    }

    internal static async Task<int> CountCompletedWorkoutDaysAsync(IQueryable<DailyWorkout> query, CancellationToken ct)
    {
        return await query.CountAsync(d =>
            d.Exercises.Any() &&
            !d.Exercises.Any(e => !e.Sets.Any() || e.Sets.Any(s => !s.IsCompleted)), ct);
    }

    internal static AdminPlanListItemResponse ToAdminPlanListItem(AdminPlanRow plan)
    {
        return new AdminPlanListItemResponse(
            plan.Id,
            plan.Name,
            plan.PlanType,
            plan.OwnerId,
            FullName(plan.OwnerFirstName, plan.OwnerLastName, plan.OwnerEmail),
            plan.OwnerEmail,
            plan.CreatedByCoachId,
            plan.CreatedByCoachId is null ? null : FullName(plan.CoachFirstName ?? string.Empty, plan.CoachLastName ?? string.Empty, plan.CoachEmail),
            plan.CoachEmail,
            plan.StartDate,
            plan.EndDate,
            plan.IsActive,
            plan.CreatedAt,
            plan.TotalWeeks,
            plan.TotalDays,
            plan.CompletedDays,
            plan.CompletionPercent,
            plan.TotalExercises,
            plan.TotalCompletedSets,
            plan.TotalVolume);
    }

    internal static string FullName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? "Unknown user" : name;
    }

    internal static string FullName(string firstName, string lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? "Unknown user" : name;
    }

    internal static bool IsSubscriptionActive(UserSubscription? subscription, DateTime now)
    {
        return subscription is null || subscription.Tier == PlanTier.Free || (subscription.ExpiresAt.HasValue && subscription.ExpiresAt.Value > now);
    }

    internal sealed record AdminPlanRow(
        Guid Id,
        string Name,
        PlanType PlanType,
        Guid OwnerId,
        string OwnerFirstName,
        string OwnerLastName,
        string OwnerEmail,
        Guid? CreatedByCoachId,
        string? CoachFirstName,
        string? CoachLastName,
        string? CoachEmail,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsActive,
        DateTime CreatedAt,
        int TotalWeeks,
        int TotalDays,
        int CompletedDays,
        int TotalExercises,
        int TotalCompletedSets,
        decimal TotalVolume)
    {
        public decimal CompletionPercent => TotalDays == 0 ? 0m : Math.Round(CompletedDays * 100m / TotalDays, 2);
    }
}
