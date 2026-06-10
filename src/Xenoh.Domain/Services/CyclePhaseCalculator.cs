using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Services;

/// <summary>
/// A single day on which period flow was logged. Used as the only signal for
/// rule-based (calendar method) cycle phase prediction.
/// </summary>
public readonly record struct CycleFlowDay(DateOnly Date, FlowIntensity Flow);

/// <summary>A predicted upcoming period span.</summary>
public sealed record PredictedPeriod(DateOnly Start, DateOnly End);

/// <summary>A predicted fertile window (ovulation − 5 … ovulation + 1).</summary>
public sealed record FertileWindow(DateOnly Start, DateOnly End);

/// <summary>
/// The full rule-based prediction for a user's current cycle state.
/// </summary>
public sealed record CyclePrediction(
    CyclePhase Phase,
    int? CycleDay,
    int EffectiveCycleLengthDays,
    int EffectivePeriodLengthDays,
    int? AverageCycleLengthDays,
    int? AveragePeriodLengthDays,
    bool IsRegular,
    int? CycleVariabilityDays,
    int? DaysUntilNextPeriod,
    int? DaysLate,
    DateOnly? LastPeriodStart,
    DateOnly? NextPeriodStart,
    IReadOnlyList<PredictedPeriod> PredictedPeriods,
    IReadOnlyList<DateOnly> OvulationDates,
    IReadOnlyList<FertileWindow> FertileWindows,
    string Confidence,
    bool NeedsData);

/// <summary>
/// Pure, deterministic calendar-method cycle prediction. No dependencies, no I/O.
/// Phases use the standard fixed 14-day luteal model: ovulation is estimated 14 days
/// before the next predicted period.
/// </summary>
public static class CyclePhaseCalculator
{
    private const int DefaultCycleLength = 28;
    private const int DefaultPeriodLength = 5;
    private const int MinPlausibleCycle = 15;
    private const int MaxPlausibleCycle = 60;

    // Two consecutive flow days more than this many days apart are treated as the
    // start of a new period (tolerates a skipped log within a single period).
    private const int PeriodGapToleranceDays = 2;

    // Beyond this multiple of the cycle length since the last period, data is too
    // stale to predict a phase reliably.
    private const int StaleCycleMultiple = 2;

    public static CyclePrediction Calculate(
        IEnumerable<CycleFlowDay> flowDays,
        int? cycleLengthOverride,
        int? periodLengthOverride,
        DateOnly today)
    {
        var ordered = flowDays
            .GroupBy(f => f.Date)
            .Select(g => g.OrderByDescending(x => x.Flow).First())
            .OrderBy(f => f.Date)
            .ToList();

        // Detect period start dates and their lengths.
        var periods = DetectPeriods(ordered);

        if (periods.Count == 0)
            return EmptyPrediction(cycleLengthOverride, periodLengthOverride);

        // Cycle lengths = gaps between consecutive period starts, filtered to plausible range.
        var cycleLengths = new List<int>();
        for (var i = 1; i < periods.Count; i++)
        {
            var gap = periods[i].Start.DayNumber - periods[i - 1].Start.DayNumber;
            if (gap is >= MinPlausibleCycle and <= MaxPlausibleCycle)
                cycleLengths.Add(gap);
        }

        // Average over the most recent up to 6 cycles.
        var recentCycleLengths = cycleLengths.TakeLast(6).ToList();
        int? computedCycle = recentCycleLengths.Count > 0
            ? (int)Math.Round(recentCycleLengths.Average())
            : null;

        var recentPeriodLengths = periods.TakeLast(6).Select(p => p.Length).ToList();
        int? computedPeriod = recentPeriodLengths.Count > 0
            ? (int)Math.Round(recentPeriodLengths.Average())
            : null;

        var effectiveCycle = Clamp(cycleLengthOverride ?? computedCycle ?? DefaultCycleLength, MinPlausibleCycle, MaxPlausibleCycle);
        var effectivePeriod = Clamp(periodLengthOverride ?? computedPeriod ?? DefaultPeriodLength, 1, 10);

        // Regularity / variability from observed cycle lengths.
        bool isRegular;
        int? variability;
        if (recentCycleLengths.Count >= 2)
        {
            var avg = recentCycleLengths.Average();
            var maxDev = recentCycleLengths.Max(c => Math.Abs(c - avg));
            variability = (int)Math.Round(recentCycleLengths.Max() - (double)recentCycleLengths.Min());
            isRegular = maxDev <= 7;
        }
        else
        {
            isRegular = true;
            variability = null;
        }

        var confidence = recentCycleLengths.Count switch
        {
            0 => "Low",
            1 => "Low",
            >= 2 and < 3 => "Medium",
            _ => isRegular ? "High" : "Medium"
        };

        var lastStart = periods[^1].Start;

        // Stale data: last period far in the past relative to the expected cycle.
        var daysSinceLastStart = today.DayNumber - lastStart.DayNumber;
        if (daysSinceLastStart > effectiveCycle * StaleCycleMultiple)
        {
            return new CyclePrediction(
                Phase: CyclePhase.Unknown,
                CycleDay: null,
                EffectiveCycleLengthDays: effectiveCycle,
                EffectivePeriodLengthDays: effectivePeriod,
                AverageCycleLengthDays: computedCycle,
                AveragePeriodLengthDays: computedPeriod,
                IsRegular: isRegular,
                CycleVariabilityDays: variability,
                DaysUntilNextPeriod: null,
                DaysLate: null,
                LastPeriodStart: lastStart,
                NextPeriodStart: null,
                PredictedPeriods: [],
                OvulationDates: [],
                FertileWindows: [],
                Confidence: "Low",
                NeedsData: true);
        }

        // Predicted future period starts (next 3 cycles from the last known start).
        var predictedPeriods = new List<PredictedPeriod>();
        var ovulationDates = new List<DateOnly>();
        var fertileWindows = new List<FertileWindow>();

        DateOnly? nextStart = null;
        for (var n = 1; n <= 4; n++)
        {
            var start = lastStart.AddDays(effectiveCycle * n);
            var end = start.AddDays(effectivePeriod - 1);
            predictedPeriods.Add(new PredictedPeriod(start, end));

            // Ovulation 14 days before this predicted start; fertile window around it.
            var ovulation = start.AddDays(-14);
            ovulationDates.Add(ovulation);
            fertileWindows.Add(new FertileWindow(ovulation.AddDays(-5), ovulation.AddDays(1)));

            if (start > today && nextStart is null)
                nextStart = start;
        }

        nextStart ??= predictedPeriods[0].Start;

        // Current phase.
        var cycleDay = daysSinceLastStart + 1; // day 1 == last period start
        var ovulationDay = effectiveCycle - 14; // cycle-day index of ovulation

        CyclePhase phase;
        int? daysLate = null;

        var loggedFlowToday = ordered.Any(f => f.Date == today);
        var withinPeriod = cycleDay <= effectivePeriod;

        if (loggedFlowToday || withinPeriod)
        {
            phase = CyclePhase.Menstrual;
        }
        else if (cycleDay > effectiveCycle)
        {
            // Period is late — stay luteal and surface how late.
            phase = CyclePhase.Luteal;
            daysLate = cycleDay - effectiveCycle;
        }
        else if (cycleDay < ovulationDay - 1)
        {
            phase = CyclePhase.Follicular;
        }
        else if (cycleDay <= ovulationDay + 1)
        {
            phase = CyclePhase.Ovulation;
        }
        else
        {
            phase = CyclePhase.Luteal;
        }

        var daysUntilNext = nextStart.Value.DayNumber - today.DayNumber;
        if (daysUntilNext < 0) daysUntilNext = 0;

        return new CyclePrediction(
            Phase: phase,
            CycleDay: cycleDay,
            EffectiveCycleLengthDays: effectiveCycle,
            EffectivePeriodLengthDays: effectivePeriod,
            AverageCycleLengthDays: computedCycle,
            AveragePeriodLengthDays: computedPeriod,
            IsRegular: isRegular,
            CycleVariabilityDays: variability,
            DaysUntilNextPeriod: daysUntilNext,
            DaysLate: daysLate,
            LastPeriodStart: lastStart,
            NextPeriodStart: nextStart,
            PredictedPeriods: predictedPeriods,
            OvulationDates: ovulationDates,
            FertileWindows: fertileWindows,
            Confidence: confidence,
            NeedsData: false);
    }

    private sealed record DetectedPeriod(DateOnly Start, int Length);

    private static List<DetectedPeriod> DetectPeriods(List<CycleFlowDay> orderedFlowDays)
    {
        var periods = new List<DetectedPeriod>();
        if (orderedFlowDays.Count == 0) return periods;

        var currentStart = orderedFlowDays[0].Date;
        var lastDay = orderedFlowDays[0].Date;

        for (var i = 1; i < orderedFlowDays.Count; i++)
        {
            var date = orderedFlowDays[i].Date;
            var gap = date.DayNumber - lastDay.DayNumber;
            if (gap > PeriodGapToleranceDays + 1)
            {
                // Close the previous period and start a new one.
                periods.Add(new DetectedPeriod(currentStart, lastDay.DayNumber - currentStart.DayNumber + 1));
                currentStart = date;
            }
            lastDay = date;
        }

        periods.Add(new DetectedPeriod(currentStart, lastDay.DayNumber - currentStart.DayNumber + 1));
        return periods;
    }

    private static CyclePrediction EmptyPrediction(int? cycleOverride, int? periodOverride) =>
        new(
            Phase: CyclePhase.Unknown,
            CycleDay: null,
            EffectiveCycleLengthDays: Clamp(cycleOverride ?? DefaultCycleLength, MinPlausibleCycle, MaxPlausibleCycle),
            EffectivePeriodLengthDays: Clamp(periodOverride ?? DefaultPeriodLength, 1, 10),
            AverageCycleLengthDays: null,
            AveragePeriodLengthDays: null,
            IsRegular: true,
            CycleVariabilityDays: null,
            DaysUntilNextPeriod: null,
            DaysLate: null,
            LastPeriodStart: null,
            NextPeriodStart: null,
            PredictedPeriods: [],
            OvulationDates: [],
            FertileWindows: [],
            Confidence: "Low",
            NeedsData: true);

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
