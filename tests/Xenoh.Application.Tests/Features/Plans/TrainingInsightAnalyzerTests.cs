using FluentAssertions;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xunit;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class TrainingInsightAnalyzerTests
{
    [Fact]
    public void Analyze_WithHighConsistencyAndStableVolume_ReturnsPositiveProgressionRecommendation()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 90m,
            NonRestDays: 10,
            MissedDays: 0,
            WarningDays: 0,
            AverageRpe: 7.5m,
            HighRpeSetCount: 0,
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 11200m)]));

        result.TrainingScore.Should().BeGreaterThanOrEqualTo(80);
        result.Insights.Should().Contain(i => i.Type == "Recommendation" && i.Severity == "Positive");
        result.Insights.Should().Contain(i => i.Type == "Consistency" && i.Severity == "Positive");
    }

    [Fact]
    public void Analyze_WithLowConsistency_ReturnsCriticalRecommendationAndLowerScore()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 40m,
            NonRestDays: 10,
            MissedDays: 4,
            WarningDays: 1,
            AverageRpe: 7m,
            HighRpeSetCount: 0,
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 9000m)]));

        result.TrainingScore.Should().BeLessThan(80);
        result.Insights.Should().Contain(i => i.Type == "Recommendation" && i.Severity == "Critical");
        result.Insights.Should().NotContain(i => i.Type == "Consistency");
    }

    [Fact]
    public void Analyze_WithLargeVolumeDrop_ReturnsVolumeWarning()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 80m,
            NonRestDays: 10,
            MissedDays: 1,
            WarningDays: 0,
            AverageRpe: null,
            HighRpeSetCount: 0,
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 7000m)]));

        result.Insights.Should().Contain(i => i.Type == "VolumeTrend" && i.Severity == "Warning");
    }

    [Fact]
    public void Analyze_WithHighRpe_ReturnsFatigueWarning()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 88m,
            NonRestDays: 10,
            MissedDays: 0,
            WarningDays: 1,
            AverageRpe: 8.7m,
            HighRpeSetCount: 6,
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 10300m)]));

        result.Insights.Should().Contain(i => i.Type == "Recommendation" && i.Severity == "Warning");
        result.Insights.Should().NotContain(i => i.Type == "FatigueRisk");
    }

    [Fact]
    public void Analyze_WithCurrentPartialWeek_DoesNotReportVolumeDrop()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 100m,
            NonRestDays: 5,
            MissedDays: 0,
            WarningDays: 0,
            AverageRpe: 7.5m,
            HighRpeSetCount: 0,
            WeeklyVolume:
            [
                new WeekVolumePoint(1, "Week 1", 10000m),
                new WeekVolumePoint(2, "Week 2", 2100m, IsPartial: true)
            ]));

        result.Insights.Should().NotContain(i =>
            i.Title == "Volume dropped sharply" || i.Title == "Volume is trending down");
        result.Insights.Should().Contain(i => i.Title == "More volume history needed");
    }

    [Fact]
    public void Analyze_WithHoldRecommendation_DoesNotRepeatConsistencyWarning()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 60m,
            NonRestDays: 5,
            MissedDays: 0,
            WarningDays: 0,
            AverageRpe: 7.5m,
            HighRpeSetCount: 0,
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m)]));

        result.Insights.Should().Contain(i => i.Title == "Hold the plan steady");
        result.Insights.Should().NotContain(i => i.Type == "Consistency");
    }

    [Fact]
    public void Analyze_WithProgrammedLighterWeek_DoesNotReportVolumeDrop()
    {
        var result = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            ConsistencyPercent: 100m,
            NonRestDays: 8,
            MissedDays: 0,
            WarningDays: 0,
            AverageRpe: 7m,
            HighRpeSetCount: 0,
            WeeklyVolume:
            [
                new WeekVolumePoint(1, "Week 1", 10000m, PlannedVolume: 10000m),
                new WeekVolumePoint(2, "Week 2", 6000m, PlannedVolume: 6000m)
            ]));

        result.Insights.Should().Contain(i => i.Title == "Planned volume reduction");
        result.Insights.Should().NotContain(i =>
            i.Title == "Volume dropped sharply" || i.Title == "Volume is trending down");
    }
}
