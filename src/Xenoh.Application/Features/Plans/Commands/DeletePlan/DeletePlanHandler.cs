using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Commands.DeletePlan;

public sealed class DeletePlanHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeletePlanCommand>
{
    public async ValueTask<Unit> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var plan = await planRepo.FindByIdAndCallerAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        planRepo.Remove(plan);
        await planRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
