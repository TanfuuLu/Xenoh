using Mediator;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.Plans.Queries.GetCoachPlans;

public sealed record GetCoachPlansQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<CoachPlanResponse>>;

public sealed record CoachPlanResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    /// <summary>"Self" = plan cá nhân của coach | "Coach" = plan tạo cho client</summary>
    string PlanType,
    Guid OwnerId,
    /// <summary>Tên của chủ plan — tên client nếu PlanType=Coach, tên coach nếu PlanType=Self</summary>
    string OwnerName,
    string OwnerEmail,
    int TotalWeeks,
    DateTime CreatedAt
);
