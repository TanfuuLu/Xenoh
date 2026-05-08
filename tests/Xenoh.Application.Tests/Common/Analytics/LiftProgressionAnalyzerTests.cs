using FluentAssertions;
using Xenoh.Application.Common.Analytics;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Common.Analytics;

public sealed class LiftProgressionAnalyzerTests
{
    [Fact]
    public void Analyze_WithNoSets_ReturnsEmptyResult()
    {
        var result = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Squat, []));

        result.E1RmSeries.Should().BeEmpty();
        result.PrTimeline.Should().BeEmpty();
        result.CurrentE1Rm.Should().BeNull();
        result.CurrentTrainingMax.Should().BeNull();
        result.IsPlateau.Should().BeFalse();
    }

    [Fact]
    public void Analyze_PrTimeline_IsMonotonicallyIncreasing()
    {
        // 100x5 (e1RM≈116.67), then 95x5 (≈110.83 — not a PR), then 105x5 (≈122.5 — PR)
        var sets = new List<CompletedLiftSet>
        {
            new(new DateOnly(2026, 1, 5), 100m, 5, null),
            new(new DateOnly(2026, 1, 12), 95m, 5, null),
            new(new DateOnly(2026, 1, 19), 105m, 5, null),
        };

        var result = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Bench, sets));

        result.PrTimeline.Should().HaveCount(2);
        result.PrTimeline[0].E1Rm.Should().BeLessThan(result.PrTimeline[1].E1Rm);
        result.CurrentE1Rm.Should().BeApproximately(122.5m, 0.05m);
        result.CurrentTrainingMax.Should().BeApproximately(110.25m, 0.05m);
    }

    [Fact]
    public void Analyze_GroupsByIsoWeek_KeepingBest()
    {
        // Two sets in the same week: best should win.
        var sets = new List<CompletedLiftSet>
        {
            new(new DateOnly(2026, 1, 5), 100m, 5, null),
            new(new DateOnly(2026, 1, 7), 110m, 5, null), // bigger
        };

        var result = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Squat, sets));

        result.E1RmSeries.Should().HaveCount(1);
        result.E1RmSeries[0].E1Rm.Should().BeApproximately(128.33m, 0.05m); // 110*(1+5/30)
    }

    [Fact]
    public void IsPlateau_TrueWhenStagnantOver4Weeks()
    {
        // Same e1RM across 4 consecutive weeks → plateau.
        var sets = new List<CompletedLiftSet>
        {
            new(new DateOnly(2026, 1, 5),  100m, 5, null),
            new(new DateOnly(2026, 1, 12), 100m, 5, null),
            new(new DateOnly(2026, 1, 19), 100m, 5, null),
            new(new DateOnly(2026, 1, 26), 100m, 5, null),
        };

        var result = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Deadlift, sets));

        result.IsPlateau.Should().BeTrue();
    }

    [Fact]
    public void IsPlateau_FalseWhenE1RmGrows()
    {
        var sets = new List<CompletedLiftSet>
        {
            new(new DateOnly(2026, 1, 5),  100m, 5, null),
            new(new DateOnly(2026, 1, 12), 105m, 5, null),
            new(new DateOnly(2026, 1, 19), 110m, 5, null),
            new(new DateOnly(2026, 1, 26), 115m, 5, null),
        };

        var result = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Squat, sets));

        result.IsPlateau.Should().BeFalse();
    }

    [Fact]
    public void BuildDotsSeries_RequiresAllThreeLifts()
    {
        var squat = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Squat,
            [new CompletedLiftSet(new DateOnly(2026, 1, 5), 150m, 5, null)]));

        var emptyBench = new LiftProgressionResult(
            CompetitionLiftType.Bench, [], [], null, null, false);
        var emptyDl = new LiftProgressionResult(
            CompetitionLiftType.Deadlift, [], [], null, null, false);

        var bw = new List<BodyweightPoint> { new(new DateOnly(2026, 1, 1), 80m) };
        var dots = LiftProgressionAnalyzer.BuildDotsSeries(squat, emptyBench, emptyDl, bw, Gender.Male);

        dots.Should().BeEmpty();
    }

    [Fact]
    public void BuildDotsSeries_ProducesPointsWhenAllInputsPresent()
    {
        var date = new DateOnly(2026, 1, 5);
        var squat = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Squat,
            [new CompletedLiftSet(date, 200m, 1, null)]));
        var bench = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Bench,
            [new CompletedLiftSet(date, 140m, 1, null)]));
        var deadlift = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(
            Guid.NewGuid(), CompetitionLiftType.Deadlift,
            [new CompletedLiftSet(date, 240m, 1, null)]));

        var bw = new List<BodyweightPoint> { new(new DateOnly(2026, 1, 1), 90m) };
        var dots = LiftProgressionAnalyzer.BuildDotsSeries(squat, bench, deadlift, bw, Gender.Male);

        dots.Should().HaveCount(1);
        dots[0].Dots.Should().BeGreaterThan(0m);
        dots[0].BodyweightKg.Should().Be(90m);
    }
}
