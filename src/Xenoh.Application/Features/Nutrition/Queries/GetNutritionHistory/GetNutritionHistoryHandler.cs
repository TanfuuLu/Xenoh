using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;

public sealed class GetNutritionHistoryHandler(
    INutritionRepository nutritionRepo,
    ICoachClientRepository coachClientRepo,
    ISubscriptionService subscriptionService,
    ICurrentUserService currentUser
) : IRequestHandler<GetNutritionHistoryQuery, List<NutritionHistoryItemResponse>>
{
    public async ValueTask<List<NutritionHistoryItemResponse>> Handle(
        GetNutritionHistoryQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        if (!await subscriptionService.CanUseAdvancedAnalyticsAsync(callerId, cancellationToken))
            throw new UnauthorizedAccessException("Advanced nutrition history requires an active paid subscription.");

        if (request.To < request.From)
            throw new InvalidOperationException("The end date must be after the start date.");

        return await nutritionRepo.GetHistoryAsync(userId, request.From, request.To, cancellationToken);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's nutrition history.");
    }
}
