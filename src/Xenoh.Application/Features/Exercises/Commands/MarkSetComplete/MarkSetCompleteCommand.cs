using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

public sealed record MarkSetCompleteCommand : IRequest<ExerciseResponse>
{
    public Guid SetId { get; init; }

    [Range(1, 1000)]
    public int? ActualReps { get; init; }

    [Range(0, 10000)]
    public decimal? ActualWeight { get; init; }

    [Range(1, 10, ErrorMessage = "RPE must be between 1 and 10.")]
    public decimal? Rpe { get; init; }
}
