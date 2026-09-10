using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.AddExerciseSet;

public sealed record AddExerciseSetCommand : IRequest<ExerciseResponse>
{
    public Guid ExerciseId { get; init; }
}
