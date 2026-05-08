using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Analytics;

/// <summary>
/// Shared engine that turns a flat history of completed competition-lift sets
/// into series + PR events the UI can chart. Used by both client (plan-scoped)
/// and coach (longitudinal-per-client) analytics.
/// </summary>
public static class LiftProgressionAnalyzer
{
    private const decimal TrainingMaxPercent = 0.9m;
    private const decimal PlateauEpsilonPct = 0.5m;
    private const int PlateauWindowWeeks = 4;

    public static LiftProgressionResult Analyze(LiftProgressionInput input)
    {
        var sets = input.Sets ?? [];
        if (sets.Count == 0)
        {
            return new LiftProgressionResult(
                input.Lift, [], [], null, null, IsPlateau: false);
        }

        // Best e1RM per ISO-week, sorted ascending by week start date.
        var weekly = sets
            .Select(s => new
            {
                Week = StartOfIsoWeek(s.Date),
                E1Rm = OneRepMaxCalculator.Estimate(s.Weight, s.Reps, input.Formula) ?? 0m,
                Set = s
            })
            .Where(x => x.E1Rm > 0m)
            .GroupBy(x => x.Week)
            .Select(g => new LiftE1RmPoint(
                g.Key,
                Math.Round(g.Max(x => x.E1Rm), 2)))
            .OrderBy(p => p.WeekStart)
            .ToList();

        // PR events: monotonically increasing by date across the whole history.
        var prTimeline = new List<LiftPrEvent>();
        decimal best = 0m;
        foreach (var s in sets.OrderBy(x => x.Date).ThenByDescending(x => x.Weight))
        {
            var e1Rm = OneRepMaxCalculator.Estimate(s.Weight, s.Reps, input.Formula);
            if (e1Rm is null) continue;
            if (e1Rm.Value <= best) continue;

            best = e1Rm.Value;
            prTimeline.Add(new LiftPrEvent(s.Date, s.Weight, s.Reps, Math.Round(best, 2)));
        }

        var current = weekly.Count > 0 ? weekly[^1].E1Rm : (decimal?)null;
        var trainingMax = current is null ? null : (decimal?)OneRepMaxCalculator.TrainingMax(current.Value, TrainingMaxPercent);

        return new LiftProgressionResult(
            input.Lift,
            weekly,
            prTimeline,
            current,
            trainingMax,
            IsPlateau: IsPlateau(weekly));
    }

    /// <summary>True if e1RM grew by less than <see cref="PlateauEpsilonPct"/>% over the trailing window.</summary>
    public static bool IsPlateau(IReadOnlyList<LiftE1RmPoint> series, int weeks = PlateauWindowWeeks)
    {
        if (series.Count < weeks) return false;
        var window = series.Skip(series.Count - weeks).ToList();
        var first = window[0].E1Rm;
        var last = window[^1].E1Rm;
        if (first <= 0m) return false;
        var changePct = (last - first) / first * 100m;
        return changePct < PlateauEpsilonPct;
    }

    /// <summary>
    /// Build a DOTS-over-time series by combining per-week best e1RM across all three lifts
    /// (Squat / Bench / Deadlift) joined with the most-recent bodyweight at each week.
    /// Returns empty when gender or bodyweight history is missing or any of the 3 lifts is empty.
    /// </summary>
    public static List<DotsOverTimePoint> BuildDotsSeries(
        LiftProgressionResult squat,
        LiftProgressionResult bench,
        LiftProgressionResult deadlift,
        IReadOnlyList<BodyweightPoint> bodyweights,
        Gender? gender)
    {
        if (gender is null) return [];
        if (squat.E1RmSeries.Count == 0 || bench.E1RmSeries.Count == 0 || deadlift.E1RmSeries.Count == 0)
            return [];
        if (bodyweights.Count == 0) return [];

        var bwSorted = bodyweights.OrderBy(b => b.Date).ToList();

        var weeks = squat.E1RmSeries.Select(p => p.WeekStart)
            .Concat(bench.E1RmSeries.Select(p => p.WeekStart))
            .Concat(deadlift.E1RmSeries.Select(p => p.WeekStart))
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        var squatLookup = squat.E1RmSeries.ToDictionary(p => p.WeekStart, p => p.E1Rm);
        var benchLookup = bench.E1RmSeries.ToDictionary(p => p.WeekStart, p => p.E1Rm);
        var deadliftLookup = deadlift.E1RmSeries.ToDictionary(p => p.WeekStart, p => p.E1Rm);

        var result = new List<DotsOverTimePoint>(weeks.Count);
        decimal lastSquat = 0m, lastBench = 0m, lastDeadlift = 0m;

        foreach (var w in weeks)
        {
            if (squatLookup.TryGetValue(w, out var sq)) lastSquat = sq;
            if (benchLookup.TryGetValue(w, out var bn)) lastBench = bn;
            if (deadliftLookup.TryGetValue(w, out var dl)) lastDeadlift = dl;

            if (lastSquat <= 0m || lastBench <= 0m || lastDeadlift <= 0m) continue;

            var bw = LookupBodyweight(bwSorted, w);
            if (bw is null) continue;

            var dots = ComputeDots(gender.Value, bw.Value, lastSquat, lastBench, lastDeadlift);
            if (dots is null) continue;

            result.Add(new DotsOverTimePoint(w, Math.Round(dots.Value, 2), bw.Value));
        }

        return result;
    }

    private static DateOnly StartOfIsoWeek(DateOnly date)
    {
        // Monday-anchored week start, matches existing analytics WeekNumber semantics.
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var diff = ((int)dt.DayOfWeek + 6) % 7;
        return DateOnly.FromDateTime(dt.AddDays(-diff));
    }

    private static decimal? LookupBodyweight(IReadOnlyList<BodyweightPoint> sorted, DateOnly atOrBefore)
    {
        decimal? value = null;
        foreach (var p in sorted)
        {
            if (p.Date <= atOrBefore) value = p.WeightKg;
            else break;
        }
        return value;
    }

    private static decimal? ComputeDots(
        Gender gender,
        decimal bodyweightKg,
        decimal squat,
        decimal bench,
        decimal deadlift)
    {
        var total = (double)(squat + bench + deadlift);
        var bw = Math.Clamp((double)bodyweightKg, 40.0, 210.0);

        double f = gender == Gender.Male
            ? 47.46178854
              + 8.472061379 * bw
              + 0.07369501861 * bw * bw
              + (-0.001395833811) * bw * bw * bw
              + 7.07665973070743e-6 * bw * bw * bw * bw
              + (-1.20804336482315e-8) * bw * bw * bw * bw * bw
            : -125.4255398
              + 13.71219419 * bw
              + (-0.03307250631) * bw * bw
              + (-0.001050400051) * bw * bw * bw
              + 9.38773881462799e-6 * bw * bw * bw * bw
              + (-2.3334613884954e-8) * bw * bw * bw * bw * bw;

        if (f <= 0) return null;
        return (decimal)(total * 500.0 / f);
    }
}

public sealed record LiftProgressionInput(
    Guid UserId,
    CompetitionLiftType Lift,
    IReadOnlyList<CompletedLiftSet> Sets,
    OneRmFormula Formula = OneRmFormula.Epley);

public sealed record CompletedLiftSet(DateOnly Date, decimal Weight, int Reps, decimal? Rpe);

public sealed record BodyweightPoint(DateOnly Date, decimal WeightKg);

public sealed record LiftProgressionResult(
    CompetitionLiftType Lift,
    List<LiftE1RmPoint> E1RmSeries,
    List<LiftPrEvent> PrTimeline,
    decimal? CurrentE1Rm,
    decimal? CurrentTrainingMax,
    bool IsPlateau);

public sealed record LiftE1RmPoint(DateOnly WeekStart, decimal E1Rm);
public sealed record LiftPrEvent(DateOnly Date, decimal Weight, int Reps, decimal E1Rm);
public sealed record DotsOverTimePoint(DateOnly WeekStart, decimal Dots, decimal BodyweightKg);
