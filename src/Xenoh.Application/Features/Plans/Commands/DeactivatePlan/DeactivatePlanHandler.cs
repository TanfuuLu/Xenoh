using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.DeactivatePlan;

public sealed class DeactivatePlanHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeactivatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(DeactivatePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await planRepo.FindForMutationAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (plan.IsActive)
        {
            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;
            await planRepo.SaveChangesAsync(cancellationToken);
        }

        return await planRepo.GetByIdForUserAsync(plan.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload plan.");
    }
}
