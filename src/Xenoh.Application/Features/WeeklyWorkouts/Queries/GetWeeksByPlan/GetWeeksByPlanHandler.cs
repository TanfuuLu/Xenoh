using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

public sealed class GetWeeksByPlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetWeeksByPlanQuery, List<WeeklyWorkoutResponse>>
{
    public async ValueTask<List<WeeklyWorkoutResponse>> Handle(GetWeeksByPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var planExists = await context.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PlanId &&
                (p.OwnerId == userId || p.CreatedByCoachId == userId), cancellationToken);

        if (!planExists)
            throw new InvalidOperationException("Plan not found.");

        return await context.WeeklyWorkouts
            .AsNoTracking()
            .Include(w => w.DailyWorkouts)
            .Where(w => w.PlanId == request.PlanId)
            .OrderBy(w => w.WeekNumber)
            .Select(w => new WeeklyWorkoutResponse(
                w.Id,
                w.WeekNumber,
                w.Name,
                w.StartDate,
                w.EndDate,
                w.PlanId,
                w.DailyWorkouts.Count,
                w.DailyWorkouts.Count(d => d.IsCompleted)
            ))
            .ToListAsync(cancellationToken);
    }
}
