using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Services;

/// <summary>A single calendar day annotated with its cycle marker.</summary>
public readonly record struct CycleDayMark(DateOnly Date, CycleDayMarker Marker);

/// <summary>A contiguous span of days sharing the same cycle marker.</summary>
public sealed record CycleMarkerSpan(DateOnly Start, DateOnly End);

/// <summary>
/// Pure, deterministic projection of menstrual / pre-menstrual markers across an
/// arbitrary date range, built on top of a <see cref="CyclePrediction"/>.
///
/// Unlike <see cref="CyclePhaseCalculator"/> (which reports the phase "as of" a single
/// day), this projects period starts forward AND backward from the last known period
/// using the effective cycle/period lengths, so future plan dates can be marked.
/// Actual logged flow days always win and are marked <see cref="CycleDayMarker.Menstrual"/>.
/// </summary>
public static class CycleDayMarkerCalculator
{
    /// <summary>Default count of luteal days before a period start treated as pre-menstrual.</summary>
    public const int DefaultPreMenstrualWindowDays = 5;

    /// <summary>
    /// Returns one entry per day in [<paramref name="from"/>, <paramref name="to"/>] whose
    /// marker is not <see cref="CycleDayMarker.None"/>. Empty when the prediction lacks an
    /// anchor period (insufficient data).
    /// </summary>
    public static IReadOnlyList<CycleDayMark> Calculate(
        CyclePrediction prediction,
        IEnumerable<CycleFlowDay> flowDays,
        DateOnly from,
        DateOnly to,
        int preMenstrualWindowDays = DefaultPreMenstrualWindowDays)
    {
        if (to < from)
            return [];

        var preWindow = Math.Clamp(preMenstrualWindowDays, 0, 10);
        var anchor = prediction.LastPeriodStart;
        if (anchor is null)
            return [];

        var cycle = Math.Max(prediction.EffectiveCycleLengthDays, 1);
        var period = Math.Max(prediction.EffectivePeriodLengthDays, 1);

        var menstrual = new HashSet<int>();
        var preMenstrual = new HashSet<int>();

        // Iterate every period start that could touch the range (including its leading
        // pre-menstrual window), projecting both backward and forward from the anchor.
        var diffFrom = from.DayNumber - anchor.Value.DayNumber;
        var kMin = (int)Math.Floor((diffFrom - preWindow - period) / (double)cycle) - 1;
        var kMax = (int)Math.Ceiling((to.DayNumber - anchor.Value.DayNumber) / (double)cycle) + 1;

        for (var k = kMin; k <= kMax; k++)
        {
            var start = anchor.Value.DayNumber + k * cycle;

            for (var d = start; d <= start + period - 1; d++)
                if (d >= from.DayNumber && d <= to.DayNumber)
                    menstrual.Add(d);

            for (var d = start - preWindow; d <= start - 1; d++)
                if (d >= from.DayNumber && d <= to.DayNumber)
                    preMenstrual.Add(d);
        }

        // Actual logged flow within the range is authoritative: always menstrual.
        foreach (var fd in flowDays)
        {
            if (fd.Date >= from && fd.Date <= to)
            {
                menstrual.Add(fd.Date.DayNumber);
                preMenstrual.Remove(fd.Date.DayNumber);
            }
        }

        var marks = new List<CycleDayMark>();
        for (var dn = from.DayNumber; dn <= to.DayNumber; dn++)
        {
            if (menstrual.Contains(dn))
                marks.Add(new CycleDayMark(DateOnly.FromDayNumber(dn), CycleDayMarker.Menstrual));
            else if (preMenstrual.Contains(dn))
                marks.Add(new CycleDayMark(DateOnly.FromDayNumber(dn), CycleDayMarker.PreMenstrual));
        }

        return marks;
    }

    /// <summary>Collapses per-day marks of a single marker into contiguous spans.</summary>
    public static IReadOnlyList<CycleMarkerSpan> CollapseSpans(
        IReadOnlyList<CycleDayMark> marks,
        CycleDayMarker marker)
    {
        var spans = new List<CycleMarkerSpan>();
        DateOnly? spanStart = null;
        DateOnly spanEnd = default;

        foreach (var mark in marks.Where(m => m.Marker == marker).OrderBy(m => m.Date))
        {
            if (spanStart is null)
            {
                spanStart = mark.Date;
                spanEnd = mark.Date;
            }
            else if (mark.Date.DayNumber == spanEnd.DayNumber + 1)
            {
                spanEnd = mark.Date;
            }
            else
            {
                spans.Add(new CycleMarkerSpan(spanStart.Value, spanEnd));
                spanStart = mark.Date;
                spanEnd = mark.Date;
            }
        }

        if (spanStart is not null)
            spans.Add(new CycleMarkerSpan(spanStart.Value, spanEnd));

        return spans;
    }
}
