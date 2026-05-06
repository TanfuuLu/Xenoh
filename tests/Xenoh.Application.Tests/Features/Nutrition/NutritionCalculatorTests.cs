using FluentAssertions;
using Xenoh.Application.Common.Nutrition;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Nutrition;

public sealed class NutritionCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 5, 5);

    [Fact]
    public void Calculate_WithCompleteBulkProfile_ReturnsMifflinTdeeAndMacroTargets()
    {
        var user = new ApplicationUser
        {
            Height = 180m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1996, 5, 5)
        };
        var profile = new NutritionProfile
        {
            ActivityLevel = ActivityLevel.Moderate,
            Goal = NutritionGoal.Bulk
        };

        var result = NutritionCalculator.Calculate(user, 80m, profile, Today);

        result.MissingFields.Should().BeEmpty();
        result.Age.Should().Be(30);
        result.Bmr.Should().Be(1780);
        result.Tdee.Should().Be(2759);
        result.RecommendedCalories.Should().Be(3059);
        result.CalorieTarget.Should().Be(3059);
        result.ProteinG.Should().Be(144m);
        result.FatG.Should().Be(64m);
        result.CarbsG.Should().Be(477m);
    }

    [Fact]
    public void Calculate_WithCutProfile_UsesHigherProteinAndCalorieDeficit()
    {
        var user = new ApplicationUser
        {
            Height = 180m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1996, 5, 5)
        };
        var profile = new NutritionProfile
        {
            ActivityLevel = ActivityLevel.Moderate,
            Goal = NutritionGoal.Cut
        };

        var result = NutritionCalculator.Calculate(user, 80m, profile, Today);

        result.RecommendedCalories.Should().Be(2259);
        result.CalorieTarget.Should().Be(2259);
        result.ProteinG.Should().Be(160m);
        result.FatG.Should().Be(64m);
        result.CarbsG.Should().Be(261m);
    }

    [Fact]
    public void Calculate_WithCustomCalories_UsesCustomTargetForMacros()
    {
        var user = new ApplicationUser
        {
            Height = 165m,
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(2001, 5, 5)
        };
        var profile = new NutritionProfile
        {
            ActivityLevel = ActivityLevel.Light,
            Goal = NutritionGoal.Maintain,
            CustomCalorieTarget = 1800
        };

        var result = NutritionCalculator.Calculate(user, 60m, profile, Today);

        result.CalorieTarget.Should().Be(1800);
        result.ProteinG.Should().Be(108m);
        result.FatG.Should().Be(48m);
        result.CarbsG.Should().Be(234m);
    }

    [Fact]
    public void Calculate_WithMaintainGoalAndLowerTargetWeight_UsesCalorieDeficit()
    {
        var user = new ApplicationUser
        {
            Height = 180m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1996, 5, 5)
        };
        var profile = new NutritionProfile
        {
            ActivityLevel = ActivityLevel.Moderate,
            Goal = NutritionGoal.Maintain,
            TargetWeightKg = 84m
        };

        var result = NutritionCalculator.Calculate(user, 87m, profile, Today);

        result.Tdee.Should().Be(2868);
        result.RecommendedCalories.Should().Be(2368);
        result.CalorieTarget.Should().Be(2368);
        result.ProteinG.Should().Be(174m);
    }

    [Fact]
    public void Calculate_WithMaintainGoalAndHigherTargetWeight_UsesCalorieSurplus()
    {
        var user = new ApplicationUser
        {
            Height = 180m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1996, 5, 5)
        };
        var profile = new NutritionProfile
        {
            ActivityLevel = ActivityLevel.Moderate,
            Goal = NutritionGoal.Maintain,
            TargetWeightKg = 90m
        };

        var result = NutritionCalculator.Calculate(user, 87m, profile, Today);

        result.Tdee.Should().Be(2868);
        result.RecommendedCalories.Should().Be(3168);
        result.CalorieTarget.Should().Be(3168);
        result.ProteinG.Should().Be(157m);
    }

    [Fact]
    public void Calculate_WithMissingProfileInputs_ReturnsMissingFields()
    {
        var user = new ApplicationUser();
        var profile = new NutritionProfile();

        var result = NutritionCalculator.Calculate(user, null, profile, Today);

        result.MissingFields.Should().BeEquivalentTo("bodyweight", "height", "dateOfBirth", "gender");
        result.Bmr.Should().BeNull();
        result.Tdee.Should().BeNull();
        result.CalorieTarget.Should().BeNull();
    }
}
