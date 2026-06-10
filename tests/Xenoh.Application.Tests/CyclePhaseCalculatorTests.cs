using FluentAssertions;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;
using Xunit;

namespace Xenoh.Application.Tests;

public class CyclePhaseCalculatorTests
{
    private static CycleFlowDay Flow(DateOnly start, int dayOffset, FlowIntensity intensity = FlowIntensity.Medium)
        => new(start.AddDays(dayOffset), intensity);

    // Builds `count` consecutive period blocks of `periodLength` days, spaced `cycleLength` apart,
    // ending so the most recent period starts on `lastStart`.
    private static List<CycleFlowDay> BuildHistory(DateOnly lastStart, int cycleLength, int periodLength, int count)
    {
        var days = new List<CycleFlowDay>();
        for (var c = 0; c < count; c++)
        {
            var periodStart = lastStart.AddDays(-cycleLength * c);
            for (var d = 0; d < periodLength; d++)
                days.Add(new CycleFlowDay(periodStart.AddDays(d), FlowIntensity.Medium));
        }
        return days;
    }

    [Fact]
    public void NoLogs_ReturnsUnknownAndNeedsData()
    {
        var today = new DateOnly(2026, 6, 10);

        var result = CyclePhaseCalculator.Calculate([], null, null, today);

        result.Phase.Should().Be(CyclePhase.Unknown);
        result.NeedsData.Should().BeTrue();
        result.PredictedPeriods.Should().BeEmpty();
        result.EffectiveCycleLengthDays.Should().Be(28);
    }

    [Fact]
    public void CurrentlyMenstruating_WhenFlowLoggedToday()
    {
        var today = new DateOnly(2026, 6, 10);
        // Period started 2 days ago and includes today.
        var days = new List<CycleFlowDay>
        {
            Flow(today, -2),
            Flow(today, -1),
            Flow(today, 0),
        };

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Menstrual);
        result.NeedsData.Should().BeFalse();
        result.CycleDay.Should().Be(3);
    }

    [Fact]
    public void RegularHistory_ComputesAverageCycleLengthAndIsRegular()
    {
        var today = new DateOnly(2026, 6, 20);
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.AverageCycleLengthDays.Should().Be(28);
        result.AveragePeriodLengthDays.Should().Be(5);
        result.IsRegular.Should().BeTrue();
        result.NeedsData.Should().BeFalse();
        result.NextPeriodStart.Should().Be(lastStart.AddDays(28)); // 2026-06-29
    }

    [Fact]
    public void RegularHistory_LutealPhaseLateInCycle()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        // Day 25 of a 28-day cycle → well past ovulation (day 14) → luteal.
        var today = lastStart.AddDays(24);

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Luteal);
        result.CycleDay.Should().Be(25);
        result.DaysLate.Should().BeNull();
    }

    [Fact]
    public void RegularHistory_OvulationAroundDay14()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        var today = lastStart.AddDays(13); // cycle day 14 → ovulation day (28 - 14)

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Ovulation);
    }

    [Fact]
    public void RegularHistory_FollicularBeforeOvulation()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        var today = lastStart.AddDays(8); // cycle day 9, after period, before ovulation

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Follicular);
    }

    [Fact]
    public void LatePeriod_StaysLutealWithDaysLate()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        // 3 days past the expected next period (cycle day 32 of a 28-day cycle), still within stale window.
        var today = lastStart.AddDays(31);

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Luteal);
        result.DaysLate.Should().Be(4); // cycleDay 32 - cycleLength 28
    }

    [Fact]
    public void IrregularHistory_FlaggedNotRegular()
    {
        var today = new DateOnly(2026, 6, 25);
        // Period starts with widely varying gaps: 22, 38, 27 days (max deviation from mean > 7).
        var s0 = new DateOnly(2026, 3, 1);
        var s1 = s0.AddDays(22);
        var s2 = s1.AddDays(38);
        var s3 = s2.AddDays(27);
        var days = new List<CycleFlowDay>();
        foreach (var start in new[] { s0, s1, s2, s3 })
            for (var d = 0; d < 5; d++)
                days.Add(new CycleFlowDay(start.AddDays(d), FlowIntensity.Medium));

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.IsRegular.Should().BeFalse();
        result.CycleVariabilityDays.Should().BeGreaterThan(7);
    }

    [Fact]
    public void GapTolerance_SkippedDayWithinPeriodIsOnePeriod()
    {
        var today = new DateOnly(2026, 6, 20);
        var start = new DateOnly(2026, 6, 1);
        // Days 1,2, (skip 3), 4,5 — a single period despite the one-day logging gap.
        var days = new List<CycleFlowDay>
        {
            Flow(start, 0),
            Flow(start, 1),
            Flow(start, 3),
            Flow(start, 4),
        };

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        // Only one period detected → no computed cycle length (single start), needs more data for trend.
        result.AverageCycleLengthDays.Should().BeNull();
        result.LastPeriodStart.Should().Be(start);
    }

    [Fact]
    public void NewPeriod_DetectedAfterLargeGap()
    {
        var today = new DateOnly(2026, 6, 20);
        var firstStart = new DateOnly(2026, 5, 1);
        var secondStart = new DateOnly(2026, 5, 29); // 28 days later
        var days = new List<CycleFlowDay>();
        for (var d = 0; d < 5; d++) days.Add(new CycleFlowDay(firstStart.AddDays(d), FlowIntensity.Medium));
        for (var d = 0; d < 5; d++) days.Add(new CycleFlowDay(secondStart.AddDays(d), FlowIntensity.Medium));

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.AverageCycleLengthDays.Should().Be(28);
        result.LastPeriodStart.Should().Be(secondStart);
    }

    [Fact]
    public void StaleData_DegradesToUnknown()
    {
        var lastStart = new DateOnly(2026, 1, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 3);
        // Today is far beyond 2x cycle length since the last period start.
        var today = new DateOnly(2026, 6, 1);

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.Phase.Should().Be(CyclePhase.Unknown);
        result.NeedsData.Should().BeTrue();
    }

    [Fact]
    public void Overrides_TakePrecedenceOverComputedValues()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        var today = lastStart.AddDays(10);

        var result = CyclePhaseCalculator.Calculate(days, cycleLengthOverride: 30, periodLengthOverride: 6, today);

        result.EffectiveCycleLengthDays.Should().Be(30);
        result.EffectivePeriodLengthDays.Should().Be(6);
        result.NextPeriodStart.Should().Be(lastStart.AddDays(30)); // uses override
    }

    [Fact]
    public void Predictions_ProduceUpcomingPeriodsAndFertileWindows()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var days = BuildHistory(lastStart, cycleLength: 28, periodLength: 5, count: 4);
        var today = lastStart.AddDays(10);

        var result = CyclePhaseCalculator.Calculate(days, null, null, today);

        result.PredictedPeriods.Should().NotBeEmpty();
        result.OvulationDates.Should().NotBeEmpty();
        result.FertileWindows.Should().NotBeEmpty();
        // First predicted period's ovulation is 14 days before its start.
        var firstPeriod = result.PredictedPeriods[0];
        result.OvulationDates[0].Should().Be(firstPeriod.Start.AddDays(-14));
    }
}
