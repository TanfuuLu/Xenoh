using FluentAssertions;
using Xenoh.Application.Common.Analytics;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Common.Analytics;

public sealed class PowerliftingInsightRulesTests
{
    [Fact]
    public void Plateau_EmitsWarning_WhenLiftIsPlateauing()
    {
        var lift = new LiftProgressionResult(
            CompetitionLiftType.Squat,
            E1RmSeries: [],
            PrTimeline: [],
            CurrentE1Rm: 200m,
            CurrentTrainingMax: 180m,
            IsPlateau: true);

        var insights = PowerliftingInsightRules.Evaluate(new PowerliftingInsightInput([lift], 0)).ToList();

        insights.Should().ContainSingle(i => i.Type == "PowerliftingPlateau" && i.Severity == "Warning");
    }

    [Fact]
    public void Imbalance_EmitsWarning_WhenBenchIsLessThanSixtyFivePercentOfSquat()
    {
        var squat = new LiftProgressionResult(CompetitionLiftType.Squat, [], [], 200m, 180m, false);
        var bench = new LiftProgressionResult(CompetitionLiftType.Bench, [], [], 100m, 90m, false); // 50%
        var deadlift = new LiftProgressionResult(CompetitionLiftType.Deadlift, [], [], 220m, 198m, false);

        var insights = PowerliftingInsightRules
            .Evaluate(new PowerliftingInsightInput([squat, bench, deadlift], 0))
            .ToList();

        insights.Should().Contain(i => i.Type == "PowerliftingImbalance" && i.Title.Contains("Bench"));
    }

    [Fact]
    public void Imbalance_DoesNotEmit_WhenLiftsAreBalanced()
    {
        var squat = new LiftProgressionResult(CompetitionLiftType.Squat, [], [], 200m, 180m, false);
        var bench = new LiftProgressionResult(CompetitionLiftType.Bench, [], [], 150m, 135m, false); // 75% — fine
        var deadlift = new LiftProgressionResult(CompetitionLiftType.Deadlift, [], [], 220m, 198m, false);

        var insights = PowerliftingInsightRules
            .Evaluate(new PowerliftingInsightInput([squat, bench, deadlift], 0))
            .ToList();

        insights.Should().NotContain(i => i.Type == "PowerliftingImbalance" && i.Title.Contains("Bench"));
    }

    [Fact]
    public void Deload_EmitsWarning_WhenHighRpeStreakHitsFour()
    {
        var insights = PowerliftingInsightRules
            .Evaluate(new PowerliftingInsightInput([], 4))
            .ToList();

        insights.Should().ContainSingle(i => i.Type == "PowerliftingDeload");
    }

    [Fact]
    public void Deload_DoesNotEmit_WhenStreakBelowThreshold()
    {
        var insights = PowerliftingInsightRules
            .Evaluate(new PowerliftingInsightInput([], 3))
            .ToList();

        insights.Should().NotContain(i => i.Type == "PowerliftingDeload");
    }
}
