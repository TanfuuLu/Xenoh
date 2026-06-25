using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class GetMealPlanForDateHandler(
    IApplicationDbContext db,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMealPlanForDateQuery, MealPlanDayResponse>
{
    public async ValueTask<MealPlanDayResponse> Handle(GetMealPlanForDateQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var day = await MealPlanQueryLoader.LoadAsNoTrackingByUserDateAsync(db, userId, request.Date, cancellationToken);
        return day is null
            ? MealPlanResponseMapper.Empty(userId, request.Date)
            : MealPlanResponseMapper.ToResponse(day);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's meal plan.");
    }
}
