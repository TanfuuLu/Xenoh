using Mediator;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetMyPlans;

public sealed record GetMyPlansQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<PlanResponse>>;
