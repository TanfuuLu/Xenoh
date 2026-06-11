using FluentAssertions;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Cycle;

public sealed class CycleContextBuilderTests : HandlerTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task TryBuildAsync_ForMaleUser_ReturnsNull()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(NewUser(Gender.Male));
        await db.SaveChangesAsync();

        var result = await CycleContextBuilder.TryBuildAsync(
            db, UserId, Today.AddDays(-7), Today.AddDays(7), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryBuildAsync_AggregatesRecentLogs_FromFeelLogs()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(NewUser(Gender.Female));
        // Three feel-logs (no flow) inside the 14-day window. Latest is today.
        db.CycleDailyLogs.AddRange(
            FeelLog(Today.AddDays(-2), CycleSymptoms.Bloating, CycleMood.Low, energy: 3),
            FeelLog(Today.AddDays(-1), CycleSymptoms.Cramps, CycleMood.Good, energy: 4),
            FeelLog(Today, CycleSymptoms.Cramps | CycleSymptoms.Fatigue, CycleMood.Low, energy: 2));
        await db.SaveChangesAsync();

        var result = await CycleContextBuilder.TryBuildAsync(
            db, UserId, Today.AddDays(-7), Today.AddDays(7), CancellationToken.None);

        result.Should().NotBeNull();
        var logs = result!.RecentLogs;
        logs.Should().NotBeNull();
        logs!.WindowDays.Should().Be(CycleContextBuilder.RecentLogWindowDays);
        logs.LoggedDays.Should().Be(3);
        logs.AvgEnergyLevel.Should().Be(3.0m);          // (3 + 4 + 2) / 3
        logs.LatestEnergyLevel.Should().Be(2);           // today's log
        logs.DominantMood.Should().Be("Low");            // 2 of 3 days
        logs.LatestMood.Should().Be("Low");
        logs.LatestLogDate.Should().Be(Today);
        logs.LatestSymptoms.Should().Equal("Cramps", "Fatigue");
        // Cramps logged on 2 days, then Bloating / Fatigue tie at 1 (alphabetical).
        logs.TopSymptoms.Should().HaveCount(3);
        logs.TopSymptoms[0].Should().Be(new CycleSymptomFrequency("Cramps", 2));
        logs.TopSymptoms[1].Should().Be(new CycleSymptomFrequency("Bloating", 1));
        logs.TopSymptoms[2].Should().Be(new CycleSymptomFrequency("Fatigue", 1));
    }

    [Fact]
    public async Task TryBuildAsync_WithNoLogsInWindow_RecentLogsIsNull()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(NewUser(Gender.Female));
        // Only a log older than the 14-day recent window.
        db.CycleDailyLogs.Add(FeelLog(Today.AddDays(-20), CycleSymptoms.Cramps, CycleMood.Low, energy: 2));
        await db.SaveChangesAsync();

        var result = await CycleContextBuilder.TryBuildAsync(
            db, UserId, Today.AddDays(-7), Today.AddDays(7), CancellationToken.None);

        result.Should().NotBeNull();
        result!.RecentLogs.Should().BeNull();
    }

    [Fact]
    public async Task TryBuildAsync_PopulatesRecentLogs_EvenWhenNeedsData()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(NewUser(Gender.Female));
        // No flow history at all -> prediction needs data, but recent feel-logs exist.
        db.CycleDailyLogs.Add(FeelLog(Today, CycleSymptoms.Fatigue, CycleMood.Neutral, energy: 3));
        await db.SaveChangesAsync();

        var result = await CycleContextBuilder.TryBuildAsync(
            db, UserId, Today.AddDays(-7), Today.AddDays(7), CancellationToken.None);

        result.Should().NotBeNull();
        result!.NeedsData.Should().BeTrue();
        result.RecentLogs.Should().NotBeNull();
        result.RecentLogs!.LoggedDays.Should().Be(1);
        result.RecentLogs.LatestSymptoms.Should().Equal("Fatigue");
    }

    private ApplicationUser NewUser(Gender gender) =>
        new() { Id = UserId, UserName = "linh@xenoh.app", Email = "linh@xenoh.app", Gender = gender };

    private CycleDailyLog FeelLog(DateOnly date, CycleSymptoms symptoms, CycleMood mood, int energy) =>
        new() { UserId = UserId, Date = date, Symptoms = symptoms, Mood = mood, EnergyLevel = energy };
}
