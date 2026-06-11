using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionProfile;

public sealed class UpdateNutritionProfileHandler(
    INutritionRepository nutritionRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateNutritionProfileCommand, NutritionProfileResponse>
{
    public async ValueTask<NutritionProfileResponse> Handle(
        UpdateNutritionProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var profile = await nutritionRepo.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new NutritionProfile { UserId = userId };
            await nutritionRepo.AddProfileAsync(profile, cancellationToken);
        }

        profile.ActivityLevel = request.ActivityLevel;
        profile.Goal = request.Goal;
        profile.TargetWeightKg = request.TargetWeightKg;
        profile.CustomCalorieTarget = request.CustomCalorieTarget;
        profile.ProteinPerKg = request.ProteinPerKg;
        profile.FatPerKg = request.FatPerKg;
        profile.UpdatedAt = DateTime.UtcNow;

        await nutritionRepo.SaveChangesAsync(cancellationToken);
        return GetNutritionSummaryHandler.ToProfileResponse(profile);
    }
}
