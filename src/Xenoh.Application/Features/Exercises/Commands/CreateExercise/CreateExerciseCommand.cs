using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Exercises.Commands.CreateExercise;

public sealed record CreateExerciseCommand : IRequest<ExerciseResponse>
{
    [Required]
    public required Guid DailyWorkoutId { get; init; }

    [Required]
    public required Guid ExerciseTemplateId { get; init; }

    [Range(1, 100)]
    public required int PlannedSets { get; init; }

    [Range(1, 1000)]
    public required int PlannedReps { get; init; }

    [Range(0, 10000)]
    public decimal? PlannedWeight { get; init; }

    public string? Notes { get; init; }
}

public sealed record ExerciseSetResponse(
    Guid Id,
    int SetNumber,
    int PlannedReps,
    decimal? PlannedWeight,
    int? ActualReps,
    decimal? ActualWeight,
    bool IsCompleted,
    DateTime? CompletedAt
);

public sealed record ExerciseResponse(
    Guid Id,
    Guid ExerciseTemplateId,
    string Name,
    string PrimaryMuscleGroup,
    List<string> SecondaryMuscleGroups,
    int PlannedSets,
    int PlannedReps,
    decimal? PlannedWeight,
    int CompletedSetsCount,
    bool IsCompleted,
    string? Notes,
    Guid DailyWorkoutId,
    List<ExerciseSetResponse> Sets,
    decimal? PersonalRecordWeight
);
