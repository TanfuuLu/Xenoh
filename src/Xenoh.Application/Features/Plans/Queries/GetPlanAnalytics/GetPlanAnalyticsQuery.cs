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
    List<MuscleGroupPoint> MuscleGroupVolume,
    List<MuscleGroupHeatmapPoint> MuscleGroupHeatmap,
    MuscleGroupBalancePoint MuscleGroupBalance
);

public sealed record WeekCompliancePoint(int WeekNumber, string WeekName, int CompletedDays, int TotalDays);
public sealed record WeekVolumePoint(int WeekNumber, string WeekName, decimal TotalVolume);
public sealed record MuscleGroupPoint(
    string MuscleGroup,
    int CompletedSets,
    decimal TotalVolume,
    decimal PrimaryVolume,
    decimal SecondaryVolume,
    decimal PercentOfTotal
);
public sealed record MuscleGroupHeatmapPoint(
    string MuscleGroup,
    decimal TotalVolume,
    List<MuscleGroupHeatmapWeekPoint> Weeks
);
public sealed record MuscleGroupHeatmapWeekPoint(int WeekNumber, string WeekName, decimal Volume);
public sealed record MuscleGroupBalancePoint(
    decimal FrontVolume,
    decimal BackVolume,
    decimal UpperVolume,
    decimal LowerVolume,
    decimal OtherVolume,
    decimal MaxVolume
);
