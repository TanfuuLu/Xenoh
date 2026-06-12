using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.SkipExercise;

public sealed record SkipExerciseCommand : IRequest<ExerciseResponse>
{
    [Required]
    public required Guid ExerciseId { get; init; }

    public required bool IsSkipped { get; init; }
}
