using Mediator;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetLastExercisePerformance;

public sealed record GetLastExercisePerformanceQuery(
    Guid ExerciseTemplateId,
    Guid DailyWorkoutId
) : IRequest<LastExercisePerformanceResponse>;

public sealed record LastExercisePerformanceResponse(
    Guid ExerciseTemplateId,
    decimal? LastActualWeight,
    int? LastActualReps,
    decimal? LastRpe,
    DateTime? PerformedAt,
    DateOnly? WorkoutDate
);
