using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.DeactivatePlan;

public sealed class DeactivatePlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<DeactivatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(DeactivatePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await context.Plans
            .Include(p => p.Owner)
            .Include(p => p.CreatedByCoach)
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        // Only the owner can deactivate
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        // Idempotent: already inactive → return current state
        if (plan.IsActive)
        {
            plan.IsActive = false;
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
