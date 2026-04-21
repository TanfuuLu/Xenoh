using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;

public sealed class GetDaysByWeekHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetDaysByWeekQuery, List<DailyWorkoutResponse>>
{
    public async ValueTask<List<DailyWorkoutResponse>> Handle(GetDaysByWeekQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var weekExists = await context.WeeklyWorkouts
            .AsNoTracking()
            .Include(w => w.Plan)
            .AnyAsync(w => w.Id == request.WeeklyWorkoutId &&
                (w.Plan.OwnerId == userId || w.Plan.CreatedByCoachId == userId), cancellationToken);

        if (!weekExists)
            throw new InvalidOperationException("Weekly workout not found.");

        return await context.DailyWorkouts
            .AsNoTracking()
            .Include(d => d.Exercises)
            .Where(d => d.WeeklyWorkoutId == request.WeeklyWorkoutId)
            .OrderBy(d => d.Date)
            .Select(d => new DailyWorkoutResponse(
                d.Id,
                d.Date,
                d.DayOfWeek.ToString(),
                d.IsCompleted,
                d.WeeklyWorkoutId,
                d.Exercises.Count,
                d.Exercises.Count(e => e.IsCompleted)
            ))
            .ToListAsync(cancellationToken);
    }
}
