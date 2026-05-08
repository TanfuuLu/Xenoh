using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Analytics;

/// <summary>
/// Orchestrator that takes a flat history of competition-lift sets for a single user
/// and produces the response-shaped <see cref="PowerliftingSection"/> + the
/// powerlifting-specific insights to merge into the existing analytics output.
/// Used by both the client (plan-scoped) and coach (longitudinal) handlers.
/// </summary>
public static class PowerliftingSectionBuilder
{
    public static async Task<(PowerliftingSection? Section, List<TrainingInsightResponse> Insights)>
        BuildAsync(
            Guid userId,
            Func<CompetitionLiftType, CancellationToken, Task<IReadOnlyList<CompletedLiftSet>>> getSets,
            IPowerliftingRepository powerliftingRepo,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct)
    {
        var squatSets = await getSets(CompetitionLiftType.Squat, ct);
        var benchSets = await getSets(CompetitionLiftType.Bench, ct);
        var deadliftSets = await getSets(CompetitionLiftType.Deadlift, ct);

        if (squatSets.Count == 0 && benchSets.Count == 0 && deadliftSets.Count == 0)
            return (null, []);

        var squat = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(userId, CompetitionLiftType.Squat, squatSets));
        var bench = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(userId, CompetitionLiftType.Bench, benchSets));
        var deadlift = LiftProgressionAnalyzer.Analyze(new LiftProgressionInput(userId, CompetitionLiftType.Deadlift, deadliftSets));

        var bodyweights = await powerliftingRepo.GetBodyweightHistoryAsync(userId, ct);
        var user = await userManager.FindByIdAsync(userId.ToString());
        var gender = user?.Gender;

        var dots = LiftProgressionAnalyzer.BuildDotsSeries(squat, bench, deadlift, bodyweights, gender);

        var section = new PowerliftingSection(
            ToResponseSeries(squat),
            ToResponseSeries(bench),
            ToResponseSeries(deadlift),
            dots.Select(d => new DotsOverTimeResponsePoint(d.WeekStart, d.Dots, d.BodyweightKg)).ToList());

        var highRpeStreak = ComputeHighRpeWeekStreak(
            squatSets.Concat(benchSets).Concat(deadliftSets).ToList());

        var insights = PowerliftingInsightRules.Evaluate(new PowerliftingInsightInput(
            new[] { squat, bench, deadlift },
            highRpeStreak)).ToList();

        return (section, insights);
    }

    private static LiftSeries ToResponseSeries(LiftProgressionResult r) =>
        new(
            r.Lift.ToString(),
            r.E1RmSeries.Select(p => new LiftE1RmResponsePoint(p.WeekStart, p.E1Rm)).ToList(),
            r.PrTimeline.Select(p => new LiftPrEventResponse(p.Date, p.Weight, p.Reps, p.E1Rm)).ToList(),
            r.CurrentE1Rm,
            r.CurrentTrainingMax,
            r.IsPlateau);

    private static int ComputeHighRpeWeekStreak(IReadOnlyList<CompletedLiftSet> sets)
    {
        if (sets.Count == 0) return 0;

        var weekly = sets
            .Where(s => s.Rpe is not null)
            .GroupBy(s => StartOfIsoWeek(s.Date))
            .Select(g => new { Week = g.Key, AvgRpe = g.Average(s => s.Rpe!.Value) })
            .OrderByDescending(x => x.Week)
            .ToList();

        int streak = 0;
        foreach (var w in weekly)
        {
            if (w.AvgRpe >= 8.5m) streak++;
            else break;
        }
        return streak;
    }

    private static DateOnly StartOfIsoWeek(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var diff = ((int)dt.DayOfWeek + 6) % 7;
        return DateOnly.FromDateTime(dt.AddDays(-diff));
    }
}
