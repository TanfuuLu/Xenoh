using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Nutrition;

public static class NutritionCalculator
{
    public static decimal ActivityMultiplier(ActivityLevel activityLevel) => activityLevel switch
    {
        ActivityLevel.Sedentary => 1.2m,
        ActivityLevel.Light => 1.375m,
        ActivityLevel.Moderate => 1.55m,
        ActivityLevel.VeryActive => 1.725m,
        ActivityLevel.Athlete => 1.9m,
        _ => 1.55m
    };

    public static NutritionCalculationResult Calculate(
        ApplicationUser user,
        decimal? bodyweightKg,
        NutritionProfile profile,
        DateOnly today)
    {
        var missing = new List<string>();

        if (bodyweightKg is null or <= 0) missing.Add("bodyweight");
        if (user.Height is null or <= 0) missing.Add("height");
        if (user.DateOfBirth is null) missing.Add("dateOfBirth");
        if (user.Gender is null) missing.Add("gender");

        if (missing.Count > 0)
        {
            return new NutritionCalculationResult(
                missing,
                bodyweightKg,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        var age = CalculateAge(user.DateOfBirth!.Value, today);
        var weight = bodyweightKg!.Value;
        var height = user.Height!.Value;

        var genderOffset = user.Gender == Gender.Male ? 5m : -161m;
        var bmr = (10m * weight) + (6.25m * height) - (5m * age) + genderOffset;
        var tdee = bmr * ActivityMultiplier(profile.ActivityLevel);
        var effectiveGoal = GetEffectiveGoal(weight, profile);
        var recommendedCalories = CalculateRecommendedCalories(tdee, effectiveGoal);

        var calorieTarget = profile.CustomCalorieTarget ?? RoundToInt(recommendedCalories);
        var proteinPerKg = profile.ProteinPerKg ?? (effectiveGoal == NutritionGoal.Cut ? 2.0m : 1.8m);
        var fatPerKg = profile.FatPerKg ?? 0.8m;
        var proteinG = Math.Round(weight * proteinPerKg, 0);
        var fatG = Math.Round(weight * fatPerKg, 0);
        var proteinCalories = proteinG * 4m;
        var fatCalories = fatG * 9m;
        var carbsG = Math.Max(0m, Math.Round((calorieTarget - proteinCalories - fatCalories) / 4m, 0));

        return new NutritionCalculationResult(
            missing,
            bodyweightKg,
            age,
            RoundToInt(bmr),
            RoundToInt(tdee),
            RoundToInt(recommendedCalories),
            calorieTarget,
            proteinG,
            carbsG,
            fatG);
    }

    private static int RoundToInt(decimal value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static NutritionGoal GetEffectiveGoal(decimal bodyweightKg, NutritionProfile profile)
    {
        if (profile.Goal == NutritionGoal.Maintain &&
            profile.TargetWeightKg is > 0 &&
            Math.Abs(profile.TargetWeightKg.Value - bodyweightKg) >= 0.1m)
        {
            return profile.TargetWeightKg.Value < bodyweightKg
                ? NutritionGoal.Cut
                : NutritionGoal.Bulk;
        }

        return profile.Goal;
    }

    private static decimal CalculateRecommendedCalories(decimal tdee, NutritionGoal effectiveGoal)
    {
        return effectiveGoal switch
        {
            NutritionGoal.Cut => tdee - 500m,
            NutritionGoal.Bulk => tdee + 300m,
            _ => tdee
        };
    }

    private static int CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (today < dateOfBirth.AddYears(age)) age--;
        return Math.Max(0, age);
    }
}
