using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Queries.GetCoachPlans;

public sealed class GetCoachPlansHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetCoachPlansQuery, List<CoachPlanResponse>>
{
    public async ValueTask<List<CoachPlanResponse>> Handle(GetCoachPlansQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        return await context.Plans
            .AsNoTracking()
            .Include(p => p.WeeklyWorkouts)
            .Include(p => p.Owner)
            .Where(p =>
                // Plan cá nhân của coach
                (p.OwnerId == coachId && p.PlanType == PlanType.Self) ||
                // Plan coach tạo cho client
                p.CreatedByCoachId == coachId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new CoachPlanResponse(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.PlanType.ToString(),
                p.OwnerId,
                $"{p.Owner.FirstName} {p.Owner.LastName}",
                p.Owner.Email!,
                p.WeeklyWorkouts.Count,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
