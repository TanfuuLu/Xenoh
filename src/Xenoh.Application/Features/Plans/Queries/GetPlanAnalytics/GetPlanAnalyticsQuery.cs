using Mediator;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

public sealed record GetPlanAnalyticsQuery(Guid PlanId) : IRequest<PlanAnalyticsResponse>;

public sealed record PlanAnalyticsResponse(
    int TotalWorkoutsCompleted,
    decimal TotalVolume,
    decimal ConsistencyPercent,
    decimal AvgSessionsPerWeek,
    int TrainingScore,
    List<TrainingInsightResponse> Insights,
    List<WeekCompliancePoint> WeeklyCompliance,
    List<WeekVolumePoint> WeeklyVolume,
    List<MuscleGroupPoint> MuscleGroupVolume,
    List<MuscleGroupHeatmapPoint> MuscleGroupHeatmap,
    MuscleGroupBalancePoint MuscleGroupBalance,
    PowerliftingSection? Powerlifting = null
);

public sealed record PowerliftingSection(
    LiftSeries Squat,
    LiftSeries Bench,
    LiftSeries Deadlift,
    List<DotsOverTimeResponsePoint> Dots
);

public sealed record LiftSeries(
    string Lift,
    List<LiftE1RmResponsePoint> E1Rm,
    List<LiftPrEventResponse> PrTimeline,
    decimal? CurrentE1Rm,
    decimal? CurrentTrainingMax,
    bool IsPlateau
);

public sealed record LiftE1RmResponsePoint(DateOnly WeekStart, decimal E1Rm);
public sealed record LiftPrEventResponse(DateOnly Date, decimal Weight, int Reps, decimal E1Rm);
public sealed record DotsOverTimeResponsePoint(DateOnly WeekStart, decimal Dots, decimal BodyweightKg);

public sealed record TrainingInsightResponse(
    string Type,
    string Severity,
    string Title,
    string Message,
    string MetricLabel,
    string MetricValue
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
