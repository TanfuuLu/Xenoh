using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Nutrition;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;

public sealed class GetNutritionSummaryHandler(
    INutritionRepository nutritionRepo,
    IBodyweightRepository bodyweightRepo,
    ICoachClientRepository coachClientRepo,
    ISubscriptionService subscriptionService,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetNutritionSummaryQuery, NutritionSummaryResponse>
{
    public async ValueTask<NutritionSummaryResponse> Handle(
        GetNutritionSummaryQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var profile = await nutritionRepo.GetProfileAsNoTrackingAsync(userId, cancellationToken)
            ?? new NutritionProfile { UserId = userId };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var bodyweight = await bodyweightRepo.GetLatestWeightAsync(userId, cancellationToken);
        var todayLog = await nutritionRepo.GetDailyLogAsNoTrackingAsync(userId, today, cancellationToken);
        var calculation = NutritionCalculator.Calculate(user, bodyweight, profile, today);
        var canUseAdvanced = await subscriptionService.CanUseAdvancedAnalyticsAsync(callerId, cancellationToken);

        return new NutritionSummaryResponse(
            userId,
            ToProfileResponse(profile),
            calculation,
            todayLog is null ? null : ToDailyLogResponse(todayLog),
            canUseAdvanced);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's nutrition data.");
    }

    public static NutritionProfileResponse ToProfileResponse(NutritionProfile profile) =>
        new(
            profile.ActivityLevel,
            profile.Goal,
            profile.TargetWeightKg,
            profile.CustomCalorieTarget,
            profile.ProteinPerKg,
            profile.FatPerKg);

    public static NutritionDailyLogResponse ToDailyLogResponse(NutritionDailyLog log) =>
        new(log.Date, log.Calories, log.ProteinG, log.CarbsG, log.FatG, log.Notes);
}
