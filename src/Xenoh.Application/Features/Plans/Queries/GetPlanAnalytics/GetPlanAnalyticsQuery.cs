using Mediator;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

public sealed record GetPlanAnalyticsQuery(Guid PlanId) : IRequest<PlanAnalyticsResponse>;

public sealed record PlanAnalyticsResponse(
    int TotalWorkoutsCompleted,
    decimal TotalVolume,
    decimal ConsistencyPercent,
    decimal AvgSessionsPerWeek,
    List<WeekCompliancePoint> WeeklyCompliance,
    List<WeekVolumePoint> WeeklyVolume,
    List<MuscleGroupPoint> MuscleGroupVolume
);

public sealed record WeekCompliancePoint(int WeekNumber, string WeekName, int CompletedDays, int TotalDays);
public sealed record WeekVolumePoint(int WeekNumber, string WeekName, decimal TotalVolume);
public sealed record MuscleGroupPoint(string MuscleGroup, int CompletedSets);
