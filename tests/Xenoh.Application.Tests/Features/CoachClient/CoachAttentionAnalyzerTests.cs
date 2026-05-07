using FluentAssertions;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class CoachAttentionAnalyzerTests
{
    [Fact]
    public void Analyze_WithNoActivePlan_ReturnsHigh()
    {
        var result = CoachAttentionAnalyzer.Analyze(null, 2);

        result.Level.Should().Be("High");
        result.Reasons.Should().Contain("No active plan");
    }

    [Fact]
    public void Analyze_WithNoWorkoutHistory_ReturnsHigh()
    {
        var result = CoachAttentionAnalyzer.Analyze(HealthySnapshot(), null);

        result.Level.Should().Be("High");
        result.Reasons.Should().Contain("No workout history");
    }

    [Fact]
    public void Analyze_WithOldWorkout_ReturnsHigh()
    {
        var result = CoachAttentionAnalyzer.Analyze(HealthySnapshot(), 8);

        result.Level.Should().Be("High");
        result.Reasons.Should().Contain("Inactive");
    }

    [Fact]
    public void Analyze_WithBehindExpectedProgress_ReturnsMedium()
    {
        var snapshot = HealthySnapshot(progress: 25, expectedProgress: 55);

        var result = CoachAttentionAnalyzer.Analyze(snapshot, 2);

        result.Level.Should().Be("Medium");
        result.Reasons.Should().Contain("Behind plan");
    }

    [Fact]
    public void Analyze_WithHealthyClient_ReturnsNone()
    {
        var result = CoachAttentionAnalyzer.Analyze(HealthySnapshot(), 2);

        result.Level.Should().Be("None");
        result.Reasons.Should().BeEmpty();
    }

    private static CoachPlanMonitoringSnapshot HealthySnapshot(
        int progress = 60,
        int expectedProgress = 70,
        int missedDays = 0) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Active Plan",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            progress,
            missedDays,
            6,
            10,
            expectedProgress);
}
