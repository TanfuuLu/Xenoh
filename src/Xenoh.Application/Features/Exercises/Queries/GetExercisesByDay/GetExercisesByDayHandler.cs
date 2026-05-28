using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

public sealed class GetExercisesByDayHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisesByDayQuery, PagedResponse<ExerciseResponse>>
{
    public async ValueTask<PagedResponse<ExerciseResponse>> Handle(
        GetExercisesByDayQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await exerciseRepo.GetByDayWithPrsAsync(
            request.DailyWorkoutId,
            userId,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
