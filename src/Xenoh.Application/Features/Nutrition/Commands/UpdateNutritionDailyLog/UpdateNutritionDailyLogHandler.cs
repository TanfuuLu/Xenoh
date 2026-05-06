using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionDailyLog;

public sealed class UpdateNutritionDailyLogHandler(
    INutritionRepository nutritionRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateNutritionDailyLogCommand, NutritionDailyLogResponse>
{
    public async ValueTask<NutritionDailyLogResponse> Handle(
        UpdateNutritionDailyLogCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var log = await nutritionRepo.GetDailyLogAsync(userId, request.Date, cancellationToken);
        if (log is null)
        {
            log = new NutritionDailyLog { UserId = userId, Date = request.Date };
            await nutritionRepo.AddDailyLogAsync(log, cancellationToken);
        }

        log.Calories = request.Calories;
        log.ProteinG = request.ProteinG;
        log.CarbsG = request.CarbsG;
        log.FatG = request.FatG;
        log.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        log.UpdatedAt = DateTime.UtcNow;

        await nutritionRepo.SaveChangesAsync(cancellationToken);
        return GetNutritionSummaryHandler.ToDailyLogResponse(log);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to edit this user's nutrition logs.");
    }
}
