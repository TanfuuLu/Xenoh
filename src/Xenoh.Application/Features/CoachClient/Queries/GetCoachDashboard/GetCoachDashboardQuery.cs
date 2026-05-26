using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;

public sealed record GetCoachDashboardQuery : IRequest<List<CoachClientDashboardResponse>>;

public sealed record CoachClientDashboardResponse(
    Guid ClientId,
    string FullName,
    string Email,
    string? AvatarUrl,
    DateOnly? LastWorkoutDate,
    int? PlanProgressPercent,
    decimal? LatestBodyweightKg,
    BigThreePRs BigThreePRs,
    Guid? ActivePlanId,
    string? ActivePlanName,
    DateOnly? ActivePlanStartDate,
    DateOnly? ActivePlanEndDate,
    int? ActivePlanProgressPercent,
    int? DaysSinceLastWorkout,
    DateOnly? LatestCompletedWorkoutDate,
    bool CompletedWorkoutToday,
    int ActivePlanCompletedWorkoutCount,
    int ActivePlanTotalWorkoutCount,
    int MissedWorkoutDays,
    int CompletedWorkoutDays,
    int TotalWorkoutDays,
    string AttentionLevel,
    List<string> AttentionReasons
);

public sealed record BigThreePRs(
    decimal? Squat,
    decimal? Bench,
    decimal? Deadlift
);

public sealed record CoachPlanMonitoringSnapshot(
    Guid OwnerId,
    Guid? ActivePlanId,
    string? ActivePlanName,
    DateOnly? ActivePlanStartDate,
    DateOnly? ActivePlanEndDate,
    int? ActivePlanProgressPercent,
    int MissedWorkoutDays,
    int CompletedWorkoutDays,
    int TotalWorkoutDays,
    int ExpectedProgressPercent
);
