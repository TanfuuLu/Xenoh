using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanDesignAnalysis;

public sealed class GetPlanDesignAnalysisHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<GetPlanDesignAnalysisQuery, PlanDesignAnalysisResponse>
{
    public async ValueTask<PlanDesignAnalysisResponse> Handle(
        GetPlanDesignAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (!await subscriptionService.CanUseAdvancedAnalyticsAsync(userId, cancellationToken))
            throw new InvalidOperationException("Advanced analytics requires an active Pro subscription.");

        return await planRepo.GetDesignAnalysisAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");
    }
}
