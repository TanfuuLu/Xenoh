using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Queries.GetCoachPlans;

public sealed class GetCoachPlansHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetCoachPlansQuery, List<CoachPlanResponse>>
{
    public async ValueTask<List<CoachPlanResponse>> Handle(
        GetCoachPlansQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        return await planRepo.GetCoachOverviewAsync(coachId, cancellationToken);
    }
}
