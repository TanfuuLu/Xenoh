using Mediator;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;

public sealed record GetDaysByWeekQuery(Guid WeeklyWorkoutId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<DailyWorkoutResponse>>;

public sealed record DailyWorkoutResponse(
    Guid Id,
    DateOnly Date,
    string DayOfWeek,
    bool IsCompleted,
    Guid WeeklyWorkoutId,
    int TotalExercises,
    int CompletedExercises,
    bool HasWarning,
    string Status
);
