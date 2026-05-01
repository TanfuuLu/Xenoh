using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.FinishExerciseTimer;

public sealed record FinishExerciseTimerCommand(Guid ExerciseId) : IRequest<ExerciseResponse>;
