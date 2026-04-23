using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.ActivatePlan;

public sealed class ActivatePlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<ActivatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(ActivatePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await context.Plans
            .Include(p => p.Owner)
            .Include(p => p.CreatedByCoach)
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        // Only the owner can activate — coaches cannot activate on behalf of clients
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        // Idempotent: already active → return current state
        if (!plan.IsActive)
        {
            // Atomic: deactivate all other active plans of this owner
            await context.Plans
                .Where(p => p.OwnerId == userId && p.IsActive && p.Id != plan.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            plan.IsActive = true;
            plan.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

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
            plan.IsActive,
            plan.CreatedAt
        );
    }
}
