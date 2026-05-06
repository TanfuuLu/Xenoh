using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionDailyLog;

public sealed class GetNutritionDailyLogHandler(
    INutritionRepository nutritionRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetNutritionDailyLogQuery, NutritionDailyLogResponse?>
{
    public async ValueTask<NutritionDailyLogResponse?> Handle(
        GetNutritionDailyLogQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var log = await nutritionRepo.GetDailyLogAsNoTrackingAsync(userId, request.Date, cancellationToken);
        return log is null ? null : GetNutritionSummaryHandler.ToDailyLogResponse(log);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's nutrition logs.");
    }
}
