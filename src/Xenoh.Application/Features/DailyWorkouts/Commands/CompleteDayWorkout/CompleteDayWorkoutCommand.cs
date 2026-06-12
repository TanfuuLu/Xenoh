using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.CompleteDayWorkout;

public sealed record CompleteDayWorkoutCommand(Guid DailyWorkoutId) : IRequest<List<ExerciseResponse>>;
