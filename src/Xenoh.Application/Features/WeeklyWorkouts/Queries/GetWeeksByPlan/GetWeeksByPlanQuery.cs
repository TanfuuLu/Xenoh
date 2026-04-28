using Mediator;

namespace Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

public sealed record GetWeeksByPlanQuery(Guid PlanId) : IRequest<List<WeeklyWorkoutResponse>>;

public sealed record WeeklyWorkoutResponse(
    Guid Id,
    int WeekNumber,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid PlanId,
    int TotalDays,
    int CompletedDays,
    bool HasWarning
);
