using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.DeleteFoodLog;

public sealed class DeleteFoodLogHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
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
        var linkedMealPlanItem = await db.MealPlanItems.FirstOrDefaultAsync(i => i.FoodLogId == log.Id, cancellationToken);
        if (linkedMealPlanItem is not null)
        {
            linkedMealPlanItem.IsChecked = false;
            linkedMealPlanItem.CheckedAt = null;
            linkedMealPlanItem.CheckedByUserId = null;
            linkedMealPlanItem.FoodLogId = null;
            linkedMealPlanItem.UpdatedAt = DateTime.UtcNow;
        }

        db.FoodLogs.Remove(log);
        await db.SaveChangesAsync(cancellationToken);

        await foodLogService.RecomputeDailyLogAsync(log.UserId, date, cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to delete this food log entry.");
    }
}
