using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanById;

public sealed class GetPlanByIdHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetPlanByIdQuery, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(
        GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await planRepo.GetByIdForUserAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");
    }
}
