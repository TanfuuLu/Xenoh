using FluentAssertions;
using Xenoh.Application.Features.Nutrition.Food;
using Xunit;

namespace Xenoh.Application.Tests.Features.Nutrition;

public sealed class FoodMacroValidatorTests
{
    // ── Physical limits ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1, 10, 10, 1)]
    [InlineData(100, -1, 10, 1)]
    [InlineData(100, 10, -1, 1)]
    [InlineData(100, 10, 10, -1)]
    public void ValidatePhysical_WithNegativeValue_Throws(
        decimal calories, decimal protein, decimal carbs, decimal fat)
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Thịt bò", calories, protein, carbs, fat);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void ValidatePhysical_WithSingleMacroOver100g_Throws()
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Whey", 400m, 120m, 0m, 0m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*no single macronutrient*");
    }

    [Fact]
    public void ValidatePhysical_WithMacrosSummingOver100g_Throws()
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Gạo tẻ", 500m, 40m, 40m, 40m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*add up to more than 100g*");
    }

    [Fact]
    public void ValidatePhysical_WithCaloriesAbovePureFat_Throws()
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Dầu ăn", 9000m, 0m, 0m, 100m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*900 kcal per 100g*");
    }

    [Fact]
    public void ValidatePhysical_WithPureFat_IsAccepted()
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Dầu ô liu", 900m, 0m, 0m, 100m);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePhysical_WithMacrosSummingToExactly100g_IsAccepted()
    {
        var act = () => FoodMacroValidator.ValidatePhysical("Đường", 400m, 0m, 100m, 0m);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePhysical_DoesNotRewriteInconsistentCalories()
    {
        // Beer: ethanol carries calories that protein/carbs/fat do not account for. A human
        // entering this deliberately keeps their number.
        var act = () => FoodMacroValidator.ValidatePhysical("Bia", 43m, 0.5m, 2.3m, 0m);

        act.Should().NotThrow();
    }

    // ── AI results ────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateAiResult_WithConsistentValues_KeepsReportedCalories()
    {
        // Gạo tẻ from the national composition table: 4(7.9) + 4(76.2) + 9(1.0) = 345.4 vs 344.
        var calories = FoodMacroValidator.ValidateAiResult("Gạo tẻ", 344m, 7.9m, 76.2m, 1.0m);

        calories.Should().Be(344m);
    }

    [Fact]
    public void ValidateAiResult_WithinTolerance_KeepsReportedCalories()
    {
        // 20% off: fiber and polyols make this drift normal, so the reported figure stands.
        var calories = FoodMacroValidator.ValidateAiResult("Rau xanh", 100m, 5m, 15m, 0m);

        calories.Should().Be(100m);
    }

    [Fact]
    public void ValidateAiResult_WhenCaloriesContradictMacros_RecomputesFromMacros()
    {
        // Claimed 500 kcal, macros only support 4(10) + 4(20) + 9(5) = 165.
        var calories = FoodMacroValidator.ValidateAiResult("Phở bò", 500m, 10m, 20m, 5m);

        calories.Should().Be(165m);
    }

    [Fact]
    public void ValidateAiResult_WhenCaloriesUnderstateMacros_RecomputesFromMacros()
    {
        var calories = FoodMacroValidator.ValidateAiResult("Hạnh nhân", 50m, 21m, 22m, 50m);

        calories.Should().Be(622m);
    }

    [Fact]
    public void ValidateAiResult_WithGenuinelyZeroCalorieFood_StaysZero()
    {
        var calories = FoodMacroValidator.ValidateAiResult("Muối", 0m, 0m, 0m, 0m);

        calories.Should().Be(0m);
    }

    [Fact]
    public void ValidateAiResult_WithZeroCaloriesButRealMacros_RecomputesFromMacros()
    {
        var calories = FoodMacroValidator.ValidateAiResult("Ức gà", 0m, 31m, 0m, 3.6m);

        calories.Should().Be(156.4m);
    }

    [Fact]
    public void ValidateAiResult_WithImpossibleValues_ThrowsBeforeRecomputing()
    {
        var act = () => FoodMacroValidator.ValidateAiResult("Bánh mì", 250m, 200m, 0m, 0m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateAiResult_IncludesFoodNameInError()
    {
        var act = () => FoodMacroValidator.ValidateAiResult("Bánh cuốn", -5m, 0m, 0m, 0m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Bánh cuốn*");
    }
}
