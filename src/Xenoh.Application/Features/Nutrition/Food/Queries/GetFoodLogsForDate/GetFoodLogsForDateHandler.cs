using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;

public sealed class GetFoodLogsForDateHandler(
    IApplicationDbContext db,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetFoodLogsForDateQuery, FoodLogsForDateResponse>
{
    public async ValueTask<FoodLogsForDateResponse> Handle(GetFoodLogsForDateQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var items = await db.FoodLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date == request.Date)
            .Include(l => l.FoodItem)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new FoodLogItemResponse(
                l.Id,
                l.FoodItemId,
                l.FoodItem.NameVi,
                l.FoodItem.NameEn,
                l.Grams,
                l.ServingLabel,
                l.ServingCount,
                l.ComputedCalories,
                l.ComputedProteinG,
                l.ComputedCarbsG,
                l.ComputedFatG
            ))
            .ToListAsync(cancellationToken);

        var totals = new FoodLogsTotals(
            items.Sum(i => i.ComputedCalories),
            items.Sum(i => i.ComputedProteinG),
            items.Sum(i => i.ComputedCarbsG),
            items.Sum(i => i.ComputedFatG)
        );

        return new FoodLogsForDateResponse(request.Date, items, totals);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's food logs.");
    }
}
