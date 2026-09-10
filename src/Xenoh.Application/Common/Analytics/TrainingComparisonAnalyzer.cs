namespace Xenoh.Application.Common.Analytics;

/// <summary>
/// Produces two equal-length, inclusive training windows. Missing repetition and RPE logs
/// remain unknown so a sparse log is never presented as poor performance or low effort.
/// </summary>
public static class TrainingComparisonAnalyzer
{
    public static TrainingComparisonResult Analyze(
        DateOnly asOf,
        int days,
        IReadOnlyList<TrainingComparisonSet> sets)
    {
        if (days is not (7 or 28 or 90))
            throw new ArgumentOutOfRangeException(nameof(days), "Comparison period must be 7, 28, or 90 days.");

        var currentStart = asOf.AddDays(1 - days);
        var previousStart = currentStart.AddDays(-days);
        var previousEnd = currentStart.AddDays(-1);

        var current = Summarize(currentStart, asOf, sets);
        var previous = Summarize(previousStart, previousEnd, sets);

        return new TrainingComparisonResult(
            current,
            previous,
            new TrainingComparisonDeltas(
                previous.CompletedSetCount > 0 ? current.CompletedSetCount - previous.CompletedSetCount : null,
                Delta(current.RepCompletionPercent, previous.RepCompletionPercent),
                Delta(current.AverageRpe, previous.AverageRpe),
                Delta(current.BestEstimatedOneRepMax, previous.BestEstimatedOneRepMax)));
    }

    public static TrainingComparisonPeriod Summarize(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<TrainingComparisonSet> allSets)
    {
        var sets = allSets
            .Where(s => s.IsCompleted && s.Date >= startDate && s.Date <= endDate)
            .ToList();
        var repEligible = sets.Where(s => s.PlannedReps > 0 && s.ActualReps is not null).ToList();
        var rpeEligible = sets.Where(s => s.Rpe is >= 1m and <= 10m).ToList();
        var estimatedMaxes = sets
            .Where(s => s.ActualWeight is > 0m && s.ActualReps is > 0)
            .Select(s => OneRepMaxCalculator.Estimate(s.ActualWeight!.Value, s.ActualReps!.Value))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        var plannedReps = repEligible.Sum(s => s.PlannedReps);
        var achievedReps = repEligible.Sum(s => Math.Min(s.ActualReps!.Value, s.PlannedReps));

        return new TrainingComparisonPeriod(
            startDate,
            endDate,
            sets.Count,
            plannedReps > 0 ? RoundPercent(achievedReps, plannedReps) : null,
            RoundPercent(repEligible.Count, sets.Count),
            rpeEligible.Count > 0 ? Math.Round(rpeEligible.Average(s => s.Rpe!.Value), 1) : null,
            RoundPercent(rpeEligible.Count, sets.Count),
            estimatedMaxes.Count > 0 ? estimatedMaxes.Max() : null);
    }

    private static decimal? Delta(decimal? current, decimal? previous) =>
        current is not null && previous is not null ? Math.Round(current.Value - previous.Value, 1) : null;

    private static decimal RoundPercent(int value, int total) =>
        total <= 0 ? 0m : Math.Round(value / (decimal)total * 100m, 1);
}

public sealed record TrainingComparisonSet(
    DateOnly Date,
    bool IsCompleted,
    int PlannedReps,
    int? ActualReps,
    decimal? ActualWeight,
    decimal? Rpe);

public sealed record TrainingComparisonPeriod(
    DateOnly StartDate,
    DateOnly EndDate,
    int CompletedSetCount,
    decimal? RepCompletionPercent,
    decimal RepCompletionCoveragePercent,
    decimal? AverageRpe,
    decimal RpeCoveragePercent,
    decimal? BestEstimatedOneRepMax);

public sealed record TrainingComparisonDeltas(
    int? CompletedSetCount,
    decimal? RepCompletionPercent,
    decimal? AverageRpe,
    decimal? BestEstimatedOneRepMax);

public sealed record TrainingComparisonResult(
    TrainingComparisonPeriod Current,
    TrainingComparisonPeriod Previous,
    TrainingComparisonDeltas Deltas);
