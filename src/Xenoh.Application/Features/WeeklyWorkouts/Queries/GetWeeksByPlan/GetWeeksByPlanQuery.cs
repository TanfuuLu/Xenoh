using Mediator;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

public sealed record GetWeeksByPlanQuery(Guid PlanId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<WeeklyWorkoutResponse>>;

public sealed record WeeklyWorkoutResponse(
    Guid Id,
    int WeekNumber,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid PlanId,
    int TotalDays,
    int CompletedDays,
    bool HasWarning,
    bool IsCompleted,
    int EffectiveTotalDays
);
