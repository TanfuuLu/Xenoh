using Mediator;
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

public sealed class GetAdminDashboardHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public async ValueTask<AdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken ct)
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
