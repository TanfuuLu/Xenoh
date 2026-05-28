using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

public sealed class GetWeeksByPlanHandler(
    IWeeklyWorkoutRepository weeklyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetWeeksByPlanQuery, PagedResponse<WeeklyWorkoutResponse>>
{
    public async ValueTask<PagedResponse<WeeklyWorkoutResponse>> Handle(
        GetWeeksByPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var accessible = await weeklyWorkoutRepo.PlanAccessibleByUserAsync(
            request.PlanId, userId, cancellationToken);

        if (!accessible)
            throw new InvalidOperationException("Plan not found.");

        return await weeklyWorkoutRepo.GetByPlanAsync(
            request.PlanId,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
