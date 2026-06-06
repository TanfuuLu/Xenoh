using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByWeek;

/// <summary>
/// Returns every exercise for all days in a week in one response, so the
/// week-analyze view can load in a single request instead of one-per-day (N+1).
/// </summary>
public sealed record GetExercisesByWeekQuery(Guid WeeklyWorkoutId)
    : IRequest<List<ExerciseResponse>>;
