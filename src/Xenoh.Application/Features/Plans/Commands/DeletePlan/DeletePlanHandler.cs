using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.DeletePlan;

public sealed class DeletePlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<DeletePlanCommand>
{
    public async ValueTask<Unit> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await context.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId &&
                (p.OwnerId == userId ||
                 (p.PlanType == PlanType.Coach && p.CreatedByCoachId == userId)), cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        context.Plans.Remove(plan);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
