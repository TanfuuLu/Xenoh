using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.SetExerciseTimerDuration;

public sealed record SetExerciseTimerDurationCommand(Guid ExerciseId, int DurationSeconds) : IRequest<ExerciseResponse>;
