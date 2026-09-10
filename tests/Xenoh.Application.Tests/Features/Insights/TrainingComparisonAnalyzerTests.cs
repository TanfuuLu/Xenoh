using FluentAssertions;
using Xenoh.Application.Common.Analytics;
using Xunit;

namespace Xenoh.Application.Tests.Features.Insights;

public sealed class TrainingComparisonAnalyzerTests
{
    [Fact]
    public void Analyze_SeparatesCurrentAndPreviousEqualLengthPeriods()
    {
        var asOf = new DateOnly(2026, 9, 10);
        var result = TrainingComparisonAnalyzer.Analyze(
            asOf,
            7,
            [
                Set(asOf.AddDays(-6), completed: true, plannedReps: 5, actualReps: 5, actualWeight: 100m, rpe: 7m),
                Set(asOf.AddDays(-1), completed: true, plannedReps: 5, actualReps: 4, actualWeight: 100m, rpe: 9m),
                Set(asOf.AddDays(-8), completed: true, plannedReps: 5, actualReps: 5, actualWeight: 90m, rpe: 8m),
                Set(asOf.AddDays(-14), completed: true, plannedReps: 5, actualReps: 5, actualWeight: 90m, rpe: null),
            ]);

        result.Current.StartDate.Should().Be(asOf.AddDays(-6));
        result.Current.EndDate.Should().Be(asOf);
        result.Previous.StartDate.Should().Be(asOf.AddDays(-13));
        result.Previous.EndDate.Should().Be(asOf.AddDays(-7));
        result.Current.CompletedSetCount.Should().Be(2);
        result.Previous.CompletedSetCount.Should().Be(1);
        result.Current.RepCompletionPercent.Should().Be(90m);
        result.Current.RpeCoveragePercent.Should().Be(100m);
        result.Previous.RpeCoveragePercent.Should().Be(100m);
        result.Current.AverageRpe.Should().Be(8m);
        result.Previous.AverageRpe.Should().Be(8m);
    }

    [Fact]
    public void Analyze_DoesNotTreatMissingRepsOrRpeAsZero()
    {
        var asOf = new DateOnly(2026, 9, 10);
        var result = TrainingComparisonAnalyzer.Analyze(
            asOf,
            7,
            [
                Set(asOf, completed: true, plannedReps: 5, actualReps: null, actualWeight: 100m, rpe: null),
                Set(asOf.AddDays(-1), completed: false, plannedReps: 5, actualReps: 5, actualWeight: 100m, rpe: 7m),
            ]);

        result.Current.CompletedSetCount.Should().Be(1);
        result.Current.RepCompletionPercent.Should().BeNull();
        result.Current.RepCompletionCoveragePercent.Should().Be(0m);
        result.Current.AverageRpe.Should().BeNull();
        result.Current.RpeCoveragePercent.Should().Be(0m);
        result.Current.BestEstimatedOneRepMax.Should().BeNull();
    }

    [Fact]
    public void Analyze_UsesOnlyCurrentAndPreviousWindowsAndReportsNoBaselineDelta()
    {
        var asOf = new DateOnly(2026, 9, 10);
        var result = TrainingComparisonAnalyzer.Analyze(
            asOf,
            7,
            [
                Set(asOf.AddDays(-6), completed: true, plannedReps: 3, actualReps: 3, actualWeight: 100m, rpe: 8m),
                Set(asOf.AddDays(-15), completed: true, plannedReps: 3, actualReps: 3, actualWeight: 80m, rpe: 8m),
            ]);

        result.Previous.CompletedSetCount.Should().Be(0);
        result.Deltas.CompletedSetCount.Should().BeNull();
        result.Deltas.BestEstimatedOneRepMax.Should().BeNull();
    }

    private static TrainingComparisonSet Set(
        DateOnly date,
        bool completed,
        int plannedReps,
        int? actualReps,
        decimal? actualWeight,
        decimal? rpe) =>
        new(date, completed, plannedReps, actualReps, actualWeight, rpe);
}
