using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;

namespace Xenoh.Application.Features.Nutrition.Food.Commands.CreateFoodLog;

public sealed class CreateFoodLogHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<CreateFoodLogCommand, FoodLogItemResponse>
{
    public async ValueTask<FoodLogItemResponse> Handle(CreateFoodLogCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var log = await foodLogService.BuildFoodLogAsync(
            userId,
            request.Date,
            request.FoodItemId,
            request.Grams,
            request.ServingLabel,
            request.ServingCount,
            cancellationToken);

        await db.FoodLogs.AddAsync(log, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await foodLogService.RecomputeDailyLogAsync(userId, request.Date, cancellationToken);

        return new FoodLogItemResponse(
            log.Id,
            log.FoodItemId,
            log.FoodItem.NameVi,
            log.FoodItem.NameEn,
            log.Grams,
            log.ServingLabelVi,
            log.ServingLabelEn,
            log.ServingCount,
            log.ComputedCalories,
            log.ComputedProteinG,
            log.ComputedCarbsG,
            log.ComputedFatG
        );
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to edit this user's food logs.");
    }
}
