using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.ReorderExercises;

public sealed record ReorderExercisesCommand : IRequest<List<ExerciseResponse>>
{
    [Required]
    public required Guid DailyWorkoutId { get; init; }

    [Required]
    [MinLength(1)]
    public required List<Guid> ExerciseIds { get; init; }
}
