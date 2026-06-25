using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Admin;

public enum AdminInsightGranularity
{
    Month = 0,
    Day = 1
}

public sealed record AdminMetricPointResponse(string Label, decimal Value);

public sealed record AdminInsightTotalsResponse(
    int TotalUsers,
    int NewUsers,
    int ActiveUsers,
    int ActivePaidSubscriptions,
    decimal Revenue,
    int PlansCreated,
    int CompletedWorkoutDays,
    int ReportsCreated,
    int AiRequests,
    int CommunityShares,
    int CommunityLoves,
    int AcceptedFriendships);

public sealed record AdminInsightsResponse(
    DateOnly From,
    DateOnly To,
    AdminInsightGranularity Granularity,
    AdminInsightTotalsResponse Totals,
    List<AdminMetricPointResponse> UserRegistrations,
    List<AdminMetricPointResponse> ActiveUsers,
    List<AdminMetricPointResponse> ActivePaidSubscriptions,
    List<AdminMetricPointResponse> Revenue,
    List<AdminMetricPointResponse> PlansCreated,
    List<AdminMetricPointResponse> CompletedWorkoutDays,
    List<AdminMetricPointResponse> ReportsCreated,
    List<AdminMetricPointResponse> AiRequests,
    List<AdminMetricPointResponse> CommunityActivity);

public sealed record AdminDashboardResponse(
    int TotalUsers,
    int NewUsersThisMonth,
    int ActiveCoaches,
    int ActivePaidSubscriptions,
    int PendingReports,
    decimal CompletedPaymentRevenueThisMonth,
    int TotalPlansCreated,
    int CompletedWorkoutDays,
    List<AdminMetricPointResponse> UserRegistrations,
    List<AdminMetricPointResponse> Revenue,
    List<AdminMetricPointResponse> SubscriptionTierDistribution,
    List<AdminMetricPointResponse> PlanCompletionTrend);

public sealed record AdminReportSummaryResponse(int Total, List<AdminStatusCountResponse> CountsByStatus);

public sealed record AdminStatusCountResponse(string Status, int Count);

public sealed record AdminUserListItemResponse(
    Guid Id,
    string Email,
    string FullName,
    List<string> Roles,
    PlanTier SubscriptionTier,
    bool IsSubscriptionActive,
    bool IsSuspended,
    DateTime CreatedAt,
    int PlanCount,
    int WorkoutHistoryCount,
    int ReportsMadeCount,
    int ReportsReceivedCount);

public sealed record AdminUserRelationshipResponse(
    Guid RelationshipId,
    Guid UserId,
    string UserName,
    string UserEmail,
    RelationshipStatus Status);

public sealed record AdminUserDetailResponse(
    Guid Id,
    string Email,
    string FullName,
    List<string> Roles,
    PlanTier SubscriptionTier,
    DateTime? SubscriptionExpiresAt,
    bool IsSubscriptionActive,
    bool IsSuspended,
    DateTime CreatedAt,
    decimal? Height,
    Gender? Gender,
    DateOnly? DateOfBirth,
    string? Bio,
    string? AvatarUrl,
    int PlanCount,
    int WorkoutHistoryCount,
    int ReportsMadeCount,
    int ReportsReceivedCount,
    AdminUserRelationshipResponse? CoachRelationship,
    List<AdminUserRelationshipResponse> ClientRelationships);

public sealed record AdminPlanListItemResponse(
    Guid Id,
    string Name,
    PlanType PlanType,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    Guid? CreatedByCoachId,
    string? CoachName,
    string? CoachEmail,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    DateTime CreatedAt,
    int TotalWeeks,
    int TotalDays,
    int CompletedDays,
    decimal CompletionPercent,
    int TotalExercises,
    int TotalCompletedSets,
    decimal TotalVolume);

public sealed record AdminPlanAnalyticsResponse(
    Guid PlanId,
    string PlanName,
    Guid OwnerId,
    string OwnerName,
    Guid? CreatedByCoachId,
    string? CoachName,
    int TotalWeeks,
    int TotalDays,
    int CompletedDays,
    decimal CompletionPercent,
    int TotalExercises,
    int TotalCompletedSets,
    decimal TotalVolume);

public sealed record AdminPaymentOrderResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    PlanTier RequestedTier,
    int DurationMonths,
    decimal Amount,
    PaymentStatus Status,
    string TransferCode,
    string? SePayTransactionId,
    string? SePayReferenceCode,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? PaidAt);

public sealed record AdminPaymentSummaryResponse(
    decimal TotalRevenue,
    decimal RevenueThisMonth,
    decimal PendingAmount,
    int CompletedOrders,
    int ActivePaidSubscriptions,
    int ProIndividualSubscriptions,
    int ProCoachSubscriptions);

public sealed record AdminSubscriptionResponse(
    Guid UserId,
    string UserName,
    string UserEmail,
    PlanTier Tier,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

public sealed record AdminAiUsageTopUserResponse(
    Guid UserId,
    string UserName,
    string UserEmail,
    PlanTier CurrentTier,
    int UsedRequests,
    DateTime? LastConsumedAt);

public sealed record AdminAiUsageSummaryResponse(
    DateOnly PeriodStart,
    int TotalUsedRequests,
    int ActiveQuotaUsers,
    List<AdminMetricPointResponse> RequestsByCurrentTier,
    List<AdminMetricPointResponse> RequestsByFeature,
    List<AdminAiUsageTopUserResponse> TopUsers);

public sealed record AdminSubscriptionAdjustmentRequest(
    PlanTier Tier,
    int? DurationMonths,
    string Reason);

public sealed record AdminSubscriptionAdjustmentResponse(
    Guid UserId,
    string UserName,
    string UserEmail,
    PlanTier Tier,
    bool IsActive,
    DateTime? ExpiresAt,
    Guid AuditLogId);

public sealed record AdminAuditLogResponse(
    Guid Id,
    Guid AdminUserId,
    string AdminUserName,
    string Action,
    string TargetType,
    Guid? TargetId,
    Guid? TargetUserId,
    string Reason,
    string BeforeSummary,
    string AfterSummary,
    DateTime CreatedAt);

public sealed record AdminMarketingTotalsResponse(
    int PageViews,
    int UniqueSessions,
    int KnownUsers,
    int Logins,
    int Registrations,
    int TotalUsageSeconds,
    decimal AverageUsageSecondsPerSession,
    int BugReportsOpen);

public sealed record AdminMarketingFlowResponse(string FromPath, string ToPath, int Count);

public sealed record AdminMarketingResponse(
    DateOnly From,
    DateOnly To,
    AdminInsightGranularity Granularity,
    AdminMarketingTotalsResponse Totals,
    List<AdminMetricPointResponse> PageViews,
    List<AdminMetricPointResponse> Logins,
    List<AdminMetricPointResponse> Registrations,
    List<AdminMetricPointResponse> UsageSeconds,
    List<AdminMetricPointResponse> TopSources,
    List<AdminMetricPointResponse> TopCampaigns,
    List<AdminMetricPointResponse> TopReferrers,
    List<AdminMetricPointResponse> TopEntryPages,
    List<AdminMarketingFlowResponse> TopFlows);
