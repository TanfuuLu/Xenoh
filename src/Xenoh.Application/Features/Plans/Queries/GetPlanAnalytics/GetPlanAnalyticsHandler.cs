using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

public sealed class GetPlanAnalyticsHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetPlanAnalyticsQuery, PlanAnalyticsResponse>
{
    public async ValueTask<PlanAnalyticsResponse> Handle(
        GetPlanAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await planRepo.GetAnalyticsAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");
    }
}
