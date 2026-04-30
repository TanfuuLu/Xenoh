using Mediator;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;

public sealed record GetDaysByWeekQuery(Guid WeeklyWorkoutId) : IRequest<List<DailyWorkoutResponse>>;

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
