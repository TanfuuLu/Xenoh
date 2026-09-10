using Xenoh.Application.Common.Analytics;

namespace Xenoh.Application.Features.Insights.Queries.GetTrainingComparison;

public sealed record TrainingComparisonResponse(
    TrainingComparisonScope Scope,
    TrainingComparisonPeriod Current,
    TrainingComparisonPeriod Previous,
    TrainingComparisonDeltas Deltas,
    IReadOnlyList<TrainingComparisonLift> AvailableLifts,
    IReadOnlyList<TrainingComparisonSession> Sessions);

public sealed record TrainingComparisonScope(
    int Days,
    DateOnly AsOf,
    DateOnly CurrentStartDate,
    DateOnly PreviousStartDate,
    Guid? ExerciseTemplateId,
    string? ExerciseName);

public sealed record TrainingComparisonLift(Guid Id, string Name);

public sealed record TrainingComparisonSession(
    Guid DailyWorkoutId,
    DateOnly Date,
    Guid ExerciseTemplateId,
    string ExerciseName,
    int CompletedSetCount,
    decimal? RepCompletionPercent,
    decimal? AverageRpe,
    decimal? BestEstimatedOneRepMax);
