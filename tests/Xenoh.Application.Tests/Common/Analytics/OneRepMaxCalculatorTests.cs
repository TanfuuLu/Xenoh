using FluentAssertions;
using Xenoh.Application.Common.Analytics;
using Xunit;

namespace Xenoh.Application.Tests.Common.Analytics;

public sealed class OneRepMaxCalculatorTests
{
    [Fact]
    public void Epley_OneRep_ReturnsWeight()
    {
        OneRepMaxCalculator.Epley(120m, 1).Should().Be(120m);
    }

    [Fact]
    public void Epley_FiveReps_AtHundred_Is_OneSixteenSixSeven()
    {
        OneRepMaxCalculator.Epley(100m, 5).Should().BeApproximately(116.67m, 0.01m);
    }

    [Fact]
    public void Brzycki_OneRep_ReturnsWeight()
    {
        OneRepMaxCalculator.Brzycki(120m, 1).Should().Be(120m);
    }

    [Fact]
    public void Brzycki_FiveReps_AtHundred_IsOneTwelveFive()
    {
        OneRepMaxCalculator.Brzycki(100m, 5).Should().BeApproximately(112.5m, 0.01m);
    }

    [Fact]
    public void Brzycki_AtTwelveReps_ReturnsNull()
    {
        OneRepMaxCalculator.Brzycki(100m, 12).Should().BeNull();
    }

    [Fact]
    public void Estimate_DefaultsToEpley()
    {
        var epley = OneRepMaxCalculator.Estimate(100m, 5)!.Value;
        epley.Should().BeApproximately(116.67m, 0.01m);
    }

    [Fact]
    public void Estimate_WithBrzyckiFormula_UsesBrzycki()
    {
        var brz = OneRepMaxCalculator.Estimate(100m, 5, OneRmFormula.Brzycki)!.Value;
        brz.Should().BeApproximately(112.5m, 0.01m);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(100, 0)]
    [InlineData(-10, 5)]
    public void Estimate_WithInvalidInputs_ReturnsNull(decimal weight, int reps)
    {
        OneRepMaxCalculator.Estimate(weight, reps).Should().BeNull();
    }

    [Fact]
    public void TrainingMax_DefaultsTo90Percent()
    {
        OneRepMaxCalculator.TrainingMax(200m).Should().Be(180m);
    }

    [Fact]
    public void TrainingMax_AcceptsCustomPercent()
    {
        OneRepMaxCalculator.TrainingMax(200m, 0.85m).Should().Be(170m);
    }
}
