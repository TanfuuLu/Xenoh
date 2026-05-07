using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Admin;

public sealed record AdminMetricPointResponse(string Label, decimal Value);

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
