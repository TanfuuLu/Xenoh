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
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 11200m)],
            MuscleGroupVolume:
            [
                new MuscleGroupPoint("Chest", 4, 2500m, 2500m, 0m, 25m),
                new MuscleGroupPoint("Back", 4, 2500m, 2500m, 0m, 25m),
                new MuscleGroupPoint("Quads", 4, 2500m, 2500m, 0m, 25m),
                new MuscleGroupPoint("Hamstrings", 4, 2500m, 2500m, 0m, 25m)
            ]));

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
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 9000m)],
            MuscleGroupVolume: [new MuscleGroupPoint("Chest", 10, 1000m, 1000m, 0m, 100m)]));

        result.TrainingScore.Should().BeLessThan(60);
        result.Insights.Should().Contain(i => i.Type == "Consistency" && i.Severity == "Critical");
        result.Insights.Should().Contain(i => i.Type == "Recommendation" && i.Severity == "Critical");
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
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 7000m)],
            MuscleGroupVolume: []));

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
            WeeklyVolume: [new WeekVolumePoint(1, "Week 1", 10000m), new WeekVolumePoint(2, "Week 2", 10300m)],
            MuscleGroupVolume: []));

        result.Insights.Should().Contain(i => i.Type == "FatigueRisk" && i.Severity == "Warning");
        result.Insights.Should().Contain(i => i.Type == "Recommendation" && i.Severity == "Warning");
    }
}
