using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.UpdateExercise;

public sealed record UpdateExerciseCommand : IRequest<ExerciseResponse>
{
    [Required]
    public required Guid ExerciseId { get; init; }

    [Range(1, 100)]
    public int? PlannedSets { get; init; }

    [Range(1, 1000)]
    public int? PlannedReps { get; init; }

    [Range(0, 10000)]
    public decimal? PlannedWeight { get; init; }

    public string? Notes { get; init; }
}
