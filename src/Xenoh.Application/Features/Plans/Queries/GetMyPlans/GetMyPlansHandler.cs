using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetMyPlans;

public sealed class GetMyPlansHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyPlansQuery, PagedResponse<PlanResponse>>
{
    public async ValueTask<PagedResponse<PlanResponse>> Handle(
        GetMyPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await planRepo.GetAllByOwnerAsync(
            userId,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
