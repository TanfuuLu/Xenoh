using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.StartExerciseTimer;

public sealed record StartExerciseTimerCommand(Guid ExerciseId) : IRequest<ExerciseResponse>;
