using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionDailyLog;

public sealed class UpdateNutritionDailyLogHandler(
    INutritionRepository nutritionRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateNutritionDailyLogCommand, NutritionDailyLogResponse>
{
    public async ValueTask<NutritionDailyLogResponse> Handle(
        UpdateNutritionDailyLogCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

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
}
