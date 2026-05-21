using Mediator;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanDesignAnalysis;

public sealed record GetPlanDesignAnalysisQuery(Guid PlanId) : IRequest<PlanDesignAnalysisResponse>;

public sealed record PlanDesignAnalysisResponse(
    PlanStructureResponse Structure,
    PlanWorkloadResponse Workload,
    List<PlannedMuscleGroupPoint> MuscleGroups,
    PlanDesignBalanceResponse Balance,
    List<MovementPatternCoveragePoint> MovementPatterns,
    List<RecoveryRiskPoint> RecoveryRisks,
    PlanVarietyResponse Variety
);

public sealed record PlanStructureResponse(
    int TotalWeeks,
    int PlannedTrainingDays,
    int PlannedRestDays,
    decimal AvgTrainingDaysPerWeek,
    int LongestTrainingStreak
);

public sealed record PlanWorkloadResponse(
    int PlannedExercises,
    int PlannedSets,
    int PlannedRepVolume,
    decimal PlannedTonnage,
    decimal AvgExercisesPerTrainingDay
);

public sealed record PlannedMuscleGroupPoint(
    string MuscleGroup,
    decimal WeightedSets,
    decimal PrimarySets,
    decimal SecondarySets,
    decimal PercentOfTotal,
    string Status
);

public sealed record PlanDesignBalanceResponse(
    decimal FrontSets,
    decimal BackSets,
    decimal UpperSets,
    decimal LowerSets,
    decimal OtherSets,
    decimal MaxSets,
    List<string> DominantMuscleGroups,
    List<string> UndertrainedMajorMuscleGroups
);

public sealed record MovementPatternCoveragePoint(
    string Pattern,
    bool IsCovered,
    int ExerciseCount,
    int PlannedSets
);

public sealed record RecoveryRiskPoint(
    string Type,
    string Severity,
    string Message,
    string Metric
);

public sealed record PlanVarietyResponse(
    int UniqueExercises,
    int RepeatedExerciseCount,
    List<RepeatedExercisePoint> TopRepeatedExercises
);

public sealed record RepeatedExercisePoint(
    string ExerciseName,
    int Count
);
