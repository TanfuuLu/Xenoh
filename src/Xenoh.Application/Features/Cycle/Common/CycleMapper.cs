using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Cycle.Common;

/// <summary>
/// Maps cycle domain types to API response DTOs and converts the symptom flag enum
/// to/from a string array.
/// </summary>
public static class CycleMapper
{
    public static IReadOnlyList<string> SymptomsToList(CycleSymptoms symptoms)
    {
        if (symptoms == CycleSymptoms.None) return [];

        return Enum.GetValues<CycleSymptoms>()
            .Where(s => s != CycleSymptoms.None && symptoms.HasFlag(s))
            .Select(s => s.ToString())
            .ToList();
    }

    public static CycleSymptoms ListToSymptoms(IEnumerable<CycleSymptoms>? values)
    {
        if (values is null) return CycleSymptoms.None;

        var result = CycleSymptoms.None;
        foreach (var v in values)
            result |= v;
        return result;
    }

    public static CycleDailyLogResponse ToResponse(CycleDailyLog log) =>
        new(
            log.Date,
            log.Flow?.ToString(),
            SymptomsToList(log.Symptoms),
            log.Mood?.ToString(),
            log.EnergyLevel,
            log.Notes);

    public static CycleOverviewResponse ToOverview(CyclePrediction p) =>
        new(
            CurrentPhase: p.Phase.ToString(),
            CycleDay: p.CycleDay,
            DaysUntilNextPeriod: p.DaysUntilNextPeriod,
            DaysLate: p.DaysLate,
            LastPeriodStart: p.LastPeriodStart,
            NextPeriodStart: p.NextPeriodStart,
            CurrentPeriodPredictedEnd: p.CurrentPeriodPredictedEnd,
            PredictedPeriods: p.PredictedPeriods.Select(x => new PredictedPeriodResponse(x.Start, x.End)).ToList(),
            OvulationDates: p.OvulationDates,
            FertileWindows: p.FertileWindows.Select(x => new FertileWindowResponse(x.Start, x.End)).ToList(),
            EffectiveCycleLengthDays: p.EffectiveCycleLengthDays,
            EffectivePeriodLengthDays: p.EffectivePeriodLengthDays,
            CycleLengthIsOverridden: p.CycleLengthIsOverridden,
            PeriodLengthIsOverridden: p.PeriodLengthIsOverridden,
            AvgCycleLengthDays: p.AverageCycleLengthDays,
            AvgPeriodLengthDays: p.AveragePeriodLengthDays,
            IsRegular: p.IsRegular,
            CycleVariabilityDays: p.CycleVariabilityDays,
            Confidence: p.Confidence,
            NeedsData: p.NeedsData);

    public static ClientCycleOverviewResponse ToClientOverview(
        CyclePrediction p, DateOnly today, IReadOnlyList<string> frequentSymptoms)
    {
        DateOnly? nextOvulation = p.OvulationDates.Where(d => d >= today).Cast<DateOnly?>().FirstOrDefault()
            ?? p.OvulationDates.Cast<DateOnly?>().FirstOrDefault();
        var fertile = p.FertileWindows.FirstOrDefault(w => w.End >= today) ?? p.FertileWindows.FirstOrDefault();

        return new(
            CurrentPhase: p.Phase.ToString(),
            CycleDay: p.CycleDay,
            NextPeriodStart: p.NextPeriodStart,
            DaysUntilNextPeriod: p.DaysUntilNextPeriod,
            DaysLate: p.DaysLate,
            EffectiveCycleLengthDays: p.EffectiveCycleLengthDays,
            EffectivePeriodLengthDays: p.EffectivePeriodLengthDays,
            AvgCycleLengthDays: p.AverageCycleLengthDays,
            AvgPeriodLengthDays: p.AveragePeriodLengthDays,
            IsRegular: p.IsRegular,
            CycleVariabilityDays: p.CycleVariabilityDays,
            LastPeriodStart: p.LastPeriodStart,
            NextOvulationDate: nextOvulation,
            FertileWindowStart: fertile?.Start,
            FertileWindowEnd: fertile?.End,
            FrequentSymptoms: frequentSymptoms,
            Confidence: p.Confidence,
            NeedsData: p.NeedsData);
    }

    /// <summary>
    /// Top recurring symptoms (by frequency) across the given logs — for the coach summary.
    /// </summary>
    public static IReadOnlyList<string> TopSymptoms(IEnumerable<CycleSymptoms> logSymptoms, int take = 5) =>
        logSymptoms
            .SelectMany(SymptomsToList)
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Take(take)
            .Select(g => g.Key)
            .ToList();
}
