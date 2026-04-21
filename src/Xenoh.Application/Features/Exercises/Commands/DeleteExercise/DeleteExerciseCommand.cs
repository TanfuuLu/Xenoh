using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Exercises.Commands.DeleteExercise;

public sealed record DeleteExerciseCommand : IRequest
{
    [Required]
    public required Guid ExerciseId { get; init; }
}
