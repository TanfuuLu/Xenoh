using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanById;

public sealed class GetPlanByIdHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetPlanByIdQuery, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await context.Plans
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.CreatedByCoach)
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId &&
                (p.OwnerId == userId || p.CreatedByCoachId == userId), cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var allDays = plan.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).ToList();

        return new PlanResponse(
            plan.Id,
            plan.Name,
            plan.StartDate,
            plan.EndDate,
            plan.PlanType.ToString(),
            plan.OwnerId,
            $"{plan.Owner.FirstName} {plan.Owner.LastName}".Trim(),
            plan.CreatedByCoachId,
            plan.CreatedByCoach is null ? null : $"{plan.CreatedByCoach.FirstName} {plan.CreatedByCoach.LastName}".Trim(),
            plan.WeeklyWorkouts.Count,
            allDays.Count,
            allDays.Count(d => d.IsCompleted),
            plan.CreatedAt
        );
    }
}
