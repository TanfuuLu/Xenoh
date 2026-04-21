using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

public sealed record GetExercisesByDayQuery(Guid DailyWorkoutId) : IRequest<List<ExerciseResponse>>;
