using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Analytics;

/// <summary>
/// Rule pack that turns powerlifting progression data into <see cref="TrainingInsightResponse"/>
/// items consumed by the same surface as the rest of the analyzer output.
/// </summary>
public static class PowerliftingInsightRules
{
    private const decimal BenchToSquatImbalanceThreshold = 0.65m;
    private const decimal DeadliftToSquatImbalanceThreshold = 1.0m; // DL should at least match Squat

    public static IEnumerable<TrainingInsightResponse> Evaluate(PowerliftingInsightInput input)
    {
        foreach (var insight in PerLiftPlateau(input)) yield return insight;
        foreach (var insight in Imbalance(input)) yield return insight;
        foreach (var insight in NoDeload(input)) yield return insight;
    }

    private static IEnumerable<TrainingInsightResponse> PerLiftPlateau(PowerliftingInsightInput input)
    {
        foreach (var lift in input.Lifts)
        {
            if (!lift.IsPlateau) continue;
            yield return new TrainingInsightResponse(
                "PowerliftingPlateau",
                "Warning",
                $"{LiftName(lift.Lift)} is plateauing",
                $"{LiftName(lift.Lift)} estimated 1RM hasn't moved in the last 4 weeks. Consider a deload or a small block of higher-rep work to break the stall.",
                "Current e1RM",
                lift.CurrentE1Rm is null ? "—" : $"{lift.CurrentE1Rm:0.#} kg");
        }
    }

    private static IEnumerable<TrainingInsightResponse> Imbalance(PowerliftingInsightInput input)
    {
        var byLift = input.Lifts.ToDictionary(l => l.Lift);
        if (!byLift.TryGetValue(CompetitionLiftType.Squat, out var squat) ||
            !byLift.TryGetValue(CompetitionLiftType.Bench, out var bench))
            yield break;

        if (squat.CurrentE1Rm is null or <= 0m || bench.CurrentE1Rm is null) yield break;

        var ratio = bench.CurrentE1Rm.Value / squat.CurrentE1Rm.Value;
        if (ratio < BenchToSquatImbalanceThreshold)
        {
            yield return new TrainingInsightResponse(
                "PowerliftingImbalance",
                "Warning",
                "Bench is lagging your squat",
                $"Your bench is {ratio * 100m:0}% of your squat. A typical balance sits around 70%. Consider an extra bench session or upper-body accessory work.",
                "Bench / Squat",
                $"{ratio * 100m:0}%");
        }

        if (byLift.TryGetValue(CompetitionLiftType.Deadlift, out var deadlift)
            && deadlift.CurrentE1Rm is not null
            && deadlift.CurrentE1Rm.Value < squat.CurrentE1Rm.Value * DeadliftToSquatImbalanceThreshold)
        {
            var dlRatio = deadlift.CurrentE1Rm.Value / squat.CurrentE1Rm.Value;
            yield return new TrainingInsightResponse(
                "PowerliftingImbalance",
                "Info",
                "Deadlift is below your squat",
                $"Deadlift is at {dlRatio * 100m:0}% of squat. Most lifters pull at least as much as they squat — check pulling volume or technique.",
                "Deadlift / Squat",
                $"{dlRatio * 100m:0}%");
        }
    }

    private static IEnumerable<TrainingInsightResponse> NoDeload(PowerliftingInsightInput input)
    {
        if (input.HighRpeWeekStreak >= 4)
        {
            yield return new TrainingInsightResponse(
                "PowerliftingDeload",
                "Warning",
                "Long stretch of high-RPE work",
                $"{input.HighRpeWeekStreak} weeks in a row averaged RPE 8.5+. Plan a deload before fatigue starts costing reps.",
                "High-RPE weeks",
                input.HighRpeWeekStreak.ToString());
        }
    }

    private static string LiftName(CompetitionLiftType lift) => lift switch
    {
        CompetitionLiftType.Squat => "Squat",
        CompetitionLiftType.Bench => "Bench",
        CompetitionLiftType.Deadlift => "Deadlift",
        _ => lift.ToString()
    };
}

public sealed record PowerliftingInsightInput(
    IReadOnlyList<LiftProgressionResult> Lifts,
    int HighRpeWeekStreak);
