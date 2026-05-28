using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;

public sealed class GetDaysByWeekHandler(
    IDailyWorkoutRepository dailyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetDaysByWeekQuery, PagedResponse<DailyWorkoutResponse>>
{
    public async ValueTask<PagedResponse<DailyWorkoutResponse>> Handle(
        GetDaysByWeekQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var accessible = await dailyWorkoutRepo.WeekAccessibleByUserAsync(
            request.WeeklyWorkoutId, userId, cancellationToken);

        if (!accessible)
            throw new InvalidOperationException("Weekly workout not found.");

        return await dailyWorkoutRepo.GetByWeekAsync(
            request.WeeklyWorkoutId,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
