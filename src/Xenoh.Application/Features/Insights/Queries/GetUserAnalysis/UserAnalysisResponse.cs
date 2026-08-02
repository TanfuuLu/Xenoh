namespace Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;

public sealed record AnalysisSection(string Headline, string Detail);

public sealed record AnalysisRecommendation(string Headline, IReadOnlyList<string> Actions);

public sealed record AnalysisTrainingDecision(
    string Verdict,
    string Rationale,
    string Prescription
);

public sealed record AnalysisPlanReview(
    string Headline,
    string DataSummary,
    string GoalFit,
    AnalysisTrainingDecision Deload,
    AnalysisTrainingDecision LoadProgression,
    string RpeGuidance,
    string VolumeGuidance,
    IReadOnlyList<string> Priorities
);

public sealed record AnalysisContent(
    AnalysisSection TrainingAdherence,
    AnalysisSection BodyMetrics,
    AnalysisSection VolumeStrength,
    AnalysisSection MuscleBalance,
    AnalysisSection EffortGap,
    AnalysisRecommendation Recommendation,
    AnalysisPlanReview PlanReview
);

public sealed record AnalysisMetrics(
    AnalysisAdherenceMetrics Adherence,
    AnalysisBodyweightMetrics Bodyweight,
    AnalysisVolumeMetrics Volume,
    IReadOnlyList<AnalysisMuscleBalancePoint> MuscleBalance,
    AnalysisEffortGapMetrics EffortGap,
    IReadOnlyList<AnalysisRecentPrEntry> RecentPrs
);

public sealed record AnalysisAdherenceMetrics(
    string? ActivePlanName,
    DateOnly? PlanStartDate,
    DateOnly? PlanEndDate,
    int TotalPlanDays,
    int CompletedPlanDays,
    int PlanCompletionPercent,
    AnalysisWeekComparison? CurrentWeek,
    AnalysisWeekComparison? PreviousWeek
);

public sealed record AnalysisWeekComparison(
    int WeekNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    int CompletedDays,
    int CompletionPercent,
    int TotalExercises,
    int CompletedExercises,
    decimal Volume
);

public sealed record AnalysisBodyweightMetrics(
    IReadOnlyList<AnalysisBodyweightPoint> Points,
    decimal? LatestWeight,
    decimal? Delta,
    string Trend
);

public sealed record AnalysisBodyweightPoint(DateOnly Date, decimal Weight);

public sealed record AnalysisVolumeMetrics(
    decimal RecentTotalVolume,
    int RecentSetCount,
    decimal CurrentWeekVolume,
    decimal PreviousWeekVolume,
    decimal WeekVolumeDelta
);

public sealed record AnalysisMuscleBalancePoint(
    string Muscle,
    int Sets,
    decimal Volume,
    int SharePercent
);

public sealed record AnalysisEffortGapMetrics(
    IReadOnlyList<AnalysisEffortGapPoint> HighRpeMisses,
    IReadOnlyList<AnalysisEffortGapPoint> LowRpeWins
);

public sealed record AnalysisEffortGapPoint(
    string Exercise,
    int Sets,
    decimal AverageRpe,
    string Pattern
);

public sealed record AnalysisRecentPrEntry(
    string Exercise,
    decimal Weight,
    int Reps,
    DateTime AchievedAt
);

public sealed record UserAnalysisResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    AnalysisContent Content,
    AnalysisMetrics Metrics
);
