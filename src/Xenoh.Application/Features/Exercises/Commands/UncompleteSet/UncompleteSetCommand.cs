using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.UncompleteSet;

public sealed record UncompleteSetCommand : IRequest<ExerciseResponse>
{
    public Guid SetId { get; init; }
}
