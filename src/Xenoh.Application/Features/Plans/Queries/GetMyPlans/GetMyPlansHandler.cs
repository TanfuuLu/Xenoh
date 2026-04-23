using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetMyPlans;

public sealed class GetMyPlansHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyPlansQuery, List<PlanResponse>>
{
    public async ValueTask<List<PlanResponse>> Handle(GetMyPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        return await context.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId)
            .Select(p => new PlanResponse(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.PlanType.ToString(),
                p.OwnerId,
                (p.Owner.FirstName + " " + p.Owner.LastName).Trim(),
                p.CreatedByCoachId,
                p.CreatedByCoach == null ? null : (p.CreatedByCoach.FirstName + " " + p.CreatedByCoach.LastName).Trim(),
                p.WeeklyWorkouts.Count,
                p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count),
                p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count(d => d.IsCompleted)),
                p.IsActive,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
