using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.Plans.Queries.GetCoachPlans;

public sealed class GetCoachPlansHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetCoachPlansQuery, PagedResponse<CoachPlanResponse>>
{
    public async ValueTask<PagedResponse<CoachPlanResponse>> Handle(
        GetCoachPlansQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        return await planRepo.GetCoachOverviewAsync(
            coachId,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
