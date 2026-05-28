using Mediator;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

public sealed record GetExercisesByDayQuery(Guid DailyWorkoutId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<ExerciseResponse>>;
