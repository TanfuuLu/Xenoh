using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.DeleteFoodLog;

public sealed class DeleteFoodLogHandler(
    IApplicationDbContext db,
    INutritionRepository nutritionRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteFoodLogCommand>
{
    public async ValueTask<Unit> Handle(DeleteFoodLogCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;

        var log = await db.FoodLogs.FirstOrDefaultAsync(l => l.Id == request.FoodLogId, cancellationToken)
            ?? throw new InvalidOperationException("Food log entry not found.");

        var userId = request.UserId ?? callerId;
        if (log.UserId != userId)
            await EnsureAccessAsync(callerId, log.UserId, cancellationToken);

        var date = log.Date;
        db.FoodLogs.Remove(log);
        await db.SaveChangesAsync(cancellationToken);

        await RecomputeDailyLogAsync(log.UserId, date, cancellationToken);

        return Unit.Value;
    }

    private async Task RecomputeDailyLogAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        var totals = await db.FoodLogs
            .Where(l => l.UserId == userId && l.Date == date)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Calories = g.Sum(l => l.ComputedCalories),
                ProteinG = g.Sum(l => l.ComputedProteinG),
                CarbsG = g.Sum(l => l.ComputedCarbsG),
                FatG = g.Sum(l => l.ComputedFatG)
            })
            .FirstOrDefaultAsync(ct);

        var dailyLog = await nutritionRepo.GetDailyLogAsync(userId, date, ct);

        if (dailyLog is null) return;

        if (totals is null)
        {
            dailyLog.Calories = 0;
            dailyLog.ProteinG = 0;
            dailyLog.CarbsG = 0;
            dailyLog.FatG = 0;
        }
        else
        {
            dailyLog.Calories = totals.Calories;
            dailyLog.ProteinG = totals.ProteinG;
            dailyLog.CarbsG = totals.CarbsG;
            dailyLog.FatG = totals.FatG;
        }

        dailyLog.UpdatedAt = DateTime.UtcNow;
        await nutritionRepo.SaveChangesAsync(ct);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to delete this food log entry.");
    }
}
