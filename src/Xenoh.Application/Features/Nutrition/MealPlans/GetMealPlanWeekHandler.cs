using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class GetMealPlanWeekHandler(
    IApplicationDbContext db,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMealPlanWeekQuery, MealPlanWeekResponse>
{
    public async ValueTask<MealPlanWeekResponse> Handle(
        GetMealPlanWeekQuery request,
        CancellationToken cancellationToken)
    {
        MealPlanWeekRules.ValidateStartDate(request.StartDate);

        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var endDate = request.StartDate.AddDays(6);
        var persistedDays = await MealPlanQueryLoader.LoadAsNoTrackingByUserDateRangeAsync(
            db, userId, request.StartDate, endDate, cancellationToken);
        var daysByDate = persistedDays.ToDictionary(d => d.Date);

        var days = Enumerable.Range(0, 7)
            .Select(offset => request.StartDate.AddDays(offset))
            .Select(date => daysByDate.TryGetValue(date, out var day)
                ? MealPlanResponseMapper.ToResponse(day)
                : MealPlanResponseMapper.Empty(userId, date))
            .ToList();

        return MealPlanWeekResponseMapper.ToResponse(request.StartDate, days);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        if (!await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct))
            throw new UnauthorizedAccessException("You do not have access to this user's meal plan.");
    }
}
