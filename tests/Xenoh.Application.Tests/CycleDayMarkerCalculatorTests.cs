using FluentAssertions;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;
using Xunit;

namespace Xenoh.Application.Tests;

public class CycleDayMarkerCalculatorTests
{
    // Builds `count` consecutive period blocks of `periodLength` days, spaced `cycleLength`
    // apart, so the most recent period starts on `lastStart`.
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
    public void NoAnchor_ReturnsEmpty()
    {
        var today = new DateOnly(2026, 6, 10);
        var prediction = CyclePhaseCalculator.Calculate([], null, null, today);

        var marks = CycleDayMarkerCalculator.Calculate(
            prediction, [], today, today.AddDays(28));

        marks.Should().BeEmpty();
    }

    [Fact]
    public void ProjectsFuturePeriodAndPreMenstrualWindow()
    {
        // Regular 28/5 history; last period started 2026-06-01.
        var lastStart = new DateOnly(2026, 6, 1);
        var today = new DateOnly(2026, 6, 10);
        var history = BuildHistory(lastStart, 28, 5, 4);
        var prediction = CyclePhaseCalculator.Calculate(history, null, null, today);

        var from = new DateOnly(2026, 6, 11);
        var to = new DateOnly(2026, 7, 10);
        var marks = CycleDayMarkerCalculator.Calculate(prediction, history, from, to, 5);

        var byDate = marks.ToDictionary(m => m.Date, m => m.Marker);

        // Next period starts 28 days after 06-01 => 2026-06-29, lasting 5 days.
        byDate.Should().ContainKey(new DateOnly(2026, 6, 29)).WhoseValue.Should().Be(CycleDayMarker.Menstrual);
        byDate[new DateOnly(2026, 7, 3)].Should().Be(CycleDayMarker.Menstrual);

        // Pre-menstrual = 5 luteal days before the period start (06-24 .. 06-28).
        byDate[new DateOnly(2026, 6, 24)].Should().Be(CycleDayMarker.PreMenstrual);
        byDate[new DateOnly(2026, 6, 28)].Should().Be(CycleDayMarker.PreMenstrual);

        // A mid-follicular day carries no marker.
        byDate.Should().NotContainKey(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public void LoggedFlowAlwaysWinsOverPreMenstrual()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var today = new DateOnly(2026, 6, 27);
        var history = BuildHistory(lastStart, 28, 5, 4);

        // The user actually started bleeding early, on 06-27 (a projected pre-menstrual day).
        history.Add(new CycleFlowDay(new DateOnly(2026, 6, 27), FlowIntensity.Light));
        var prediction = CyclePhaseCalculator.Calculate(history, null, null, today);

        var marks = CycleDayMarkerCalculator.Calculate(
            prediction, history, new DateOnly(2026, 6, 27), new DateOnly(2026, 6, 27), 5);

        marks.Should().ContainSingle()
            .Which.Marker.Should().Be(CycleDayMarker.Menstrual);
    }

    [Fact]
    public void CollapseSpans_GroupsContiguousDays()
    {
        var lastStart = new DateOnly(2026, 6, 1);
        var today = new DateOnly(2026, 6, 10);
        var history = BuildHistory(lastStart, 28, 5, 4);
        var prediction = CyclePhaseCalculator.Calculate(history, null, null, today);

        var marks = CycleDayMarkerCalculator.Calculate(
            prediction, history, new DateOnly(2026, 6, 11), new DateOnly(2026, 7, 5), 5);

        var menstrualSpans = CycleDayMarkerCalculator.CollapseSpans(marks, CycleDayMarker.Menstrual);

        menstrualSpans.Should().ContainSingle(s =>
            s.Start == new DateOnly(2026, 6, 29) && s.End == new DateOnly(2026, 7, 3));
    }
}
