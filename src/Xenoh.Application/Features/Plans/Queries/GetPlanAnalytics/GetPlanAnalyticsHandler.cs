using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

public sealed class GetPlanAnalyticsHandler(
    IPlanRepository planRepo,
    IPowerliftingRepository powerliftingRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<GetPlanAnalyticsQuery, PlanAnalyticsResponse>
{
    public async ValueTask<PlanAnalyticsResponse> Handle(
        GetPlanAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (!await subscriptionService.CanUseAdvancedAnalyticsAsync(userId, cancellationToken))
            throw new InvalidOperationException("Advanced analytics requires an active Pro subscription.");

        var analytics = await planRepo.GetAnalyticsAsync(request.PlanId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var hasCompetitionLifts = await powerliftingRepo
            .PlanHasCompetitionLiftsAsync(request.PlanId, userId, cancellationToken);

        if (!hasCompetitionLifts)
            return analytics;

        var (section, insights) = await PowerliftingSectionBuilder.BuildAsync(
            userId,
            (lift, ct) => powerliftingRepo.GetCompletedSetsForPlanAsync(request.PlanId, userId, lift, ct),
            powerliftingRepo,
            userManager,
            cancellationToken);

        if (section is null)
            return analytics;

        var mergedInsights = analytics.Insights.Concat(insights).ToList();

        return analytics with
        {
            Insights = mergedInsights,
            Powerlifting = section
        };
    }
}
