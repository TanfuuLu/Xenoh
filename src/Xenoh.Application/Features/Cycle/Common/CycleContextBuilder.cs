using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Cycle.Common;

public sealed record CycleSpan(DateOnly Start, DateOnly End);

/// <summary>
/// Compact, AI- and UI-friendly view of a user's cycle state plus the menstrual /
/// pre-menstrual day spans that fall inside a requested date window. Serialized into
/// AI snapshots and returned by the day-markers endpoint.
/// </summary>
/// <summary>How many days a given symptom was logged inside the recent-logs window.</summary>
public sealed record CycleSymptomFrequency(string Symptom, int Days);

/// <summary>
/// Compact summary of how the user has actually felt over the recent-logs window
/// (logged symptoms, mood, energy) — distinct from the calendar phase. Lets AI
/// coaching respond to lived experience, not just the predicted phase. Null when the
/// user has logged nothing in the window.
/// </summary>
public sealed record AiCycleRecentLogs(
    int WindowDays,
    int LoggedDays,
    decimal? AvgEnergyLevel,
    int? LatestEnergyLevel,
    string? DominantMood,
    string? LatestMood,
    IReadOnlyList<CycleSymptomFrequency> TopSymptoms,
    IReadOnlyList<string> LatestSymptoms,
    DateOnly? LatestLogDate);

public sealed record AiCycleContext(
    bool NeedsData,
    string CurrentPhase,
    int? CycleDay,
    int EffectiveCycleLengthDays,
    int EffectivePeriodLengthDays,
    int? AvgCycleLengthDays,
    int? AvgPeriodLengthDays,
    bool IsRegular,
    int? DaysUntilNextPeriod,
    int? DaysLate,
    int PreMenstrualWindowDays,
    IReadOnlyList<CycleSpan> MenstrualSpans,
    IReadOnlyList<CycleSpan> PreMenstrualSpans,
    AiCycleRecentLogs? RecentLogs);

/// <summary>
/// Builds an <see cref="AiCycleContext"/> for a user over a date window. Returns
/// <c>null</c> for non-female profiles so callers can omit cycle handling entirely.
/// Reuses the rule-based <see cref="CyclePhaseCalculator"/> + <see cref="CycleDayMarkerCalculator"/>.
/// </summary>
public static class CycleContextBuilder
{
    public const int PreMenstrualWindowDays = CycleDayMarkerCalculator.DefaultPreMenstrualWindowDays;

    /// <summary>Trailing window (days, inclusive of today) for recent symptom / mood / energy logs.</summary>
    public const int RecentLogWindowDays = 14;

    public static async Task<AiCycleContext?> TryBuildAsync(
        IApplicationDbContext db,
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var gender = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Gender)
            .FirstOrDefaultAsync(ct);

        if (gender != Gender.Female)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-400);

        var flowRows = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Flow != null && l.Date >= since && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Flow })
            .ToListAsync(ct);

        var settings = await db.CycleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        var flowDays = flowRows.Select(x => new CycleFlowDay(x.Date, x.Flow!.Value)).ToList();

        // Recent symptom / mood / energy logs — how the user has actually felt lately.
        // Built independently of flow history so it is available even before a cycle can be predicted.
        var recentSince = today.AddDays(-(RecentLogWindowDays - 1));
        var recentRows = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= recentSince && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new CycleLogRow(l.Date, l.Symptoms, l.Mood, l.EnergyLevel))
            .ToListAsync(ct);

        var recentLogs = BuildRecentLogs(recentRows);

        var prediction = CyclePhaseCalculator.Calculate(
            flowDays,
            settings?.AverageCycleLengthOverride,
            settings?.AveragePeriodLengthOverride,
            today);

        if (prediction.NeedsData || prediction.LastPeriodStart is null)
        {
            return new AiCycleContext(
                NeedsData: true,
                CurrentPhase: prediction.Phase.ToString(),
                CycleDay: prediction.CycleDay,
                EffectiveCycleLengthDays: prediction.EffectiveCycleLengthDays,
                EffectivePeriodLengthDays: prediction.EffectivePeriodLengthDays,
                AvgCycleLengthDays: prediction.AverageCycleLengthDays,
                AvgPeriodLengthDays: prediction.AveragePeriodLengthDays,
                IsRegular: prediction.IsRegular,
                DaysUntilNextPeriod: prediction.DaysUntilNextPeriod,
                DaysLate: prediction.DaysLate,
                PreMenstrualWindowDays: PreMenstrualWindowDays,
                MenstrualSpans: [],
                PreMenstrualSpans: [],
                RecentLogs: recentLogs);
        }

        var marks = CycleDayMarkerCalculator.Calculate(prediction, flowDays, from, to, PreMenstrualWindowDays);

        return new AiCycleContext(
            NeedsData: false,
            CurrentPhase: prediction.Phase.ToString(),
            CycleDay: prediction.CycleDay,
            EffectiveCycleLengthDays: prediction.EffectiveCycleLengthDays,
            EffectivePeriodLengthDays: prediction.EffectivePeriodLengthDays,
            AvgCycleLengthDays: prediction.AverageCycleLengthDays,
            AvgPeriodLengthDays: prediction.AveragePeriodLengthDays,
            IsRegular: prediction.IsRegular,
            DaysUntilNextPeriod: prediction.DaysUntilNextPeriod,
            DaysLate: prediction.DaysLate,
            PreMenstrualWindowDays: PreMenstrualWindowDays,
            MenstrualSpans: CycleDayMarkerCalculator.CollapseSpans(marks, CycleDayMarker.Menstrual)
                .Select(s => new CycleSpan(s.Start, s.End)).ToList(),
            PreMenstrualSpans: CycleDayMarkerCalculator.CollapseSpans(marks, CycleDayMarker.PreMenstrual)
                .Select(s => new CycleSpan(s.Start, s.End)).ToList(),
            RecentLogs: recentLogs);
    }

    private sealed record CycleLogRow(DateOnly Date, CycleSymptoms Symptoms, CycleMood? Mood, int? EnergyLevel);

    /// <summary>
    /// Aggregates the recent daily logs into a compact feel-summary. Returns null when
    /// the user has logged nothing in the window. "Latest" values come from the single
    /// most recent logged day; TopSymptoms / DominantMood / AvgEnergyLevel span the window.
    /// </summary>
    private static AiCycleRecentLogs? BuildRecentLogs(IReadOnlyList<CycleLogRow> rows)
    {
        if (rows.Count == 0)
            return null;

        var latest = rows[^1];

        var energies = rows.Where(r => r.EnergyLevel.HasValue).Select(r => r.EnergyLevel!.Value).ToList();
        var moods = rows.Where(r => r.Mood.HasValue).Select(r => r.Mood!.Value).ToList();

        var topSymptoms = rows
            .SelectMany(r => CycleMapper.SymptomsToList(r.Symptoms))
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Take(5)
            .Select(g => new CycleSymptomFrequency(g.Key, g.Count()))
            .ToList();

        return new AiCycleRecentLogs(
            WindowDays: RecentLogWindowDays,
            LoggedDays: rows.Count,
            AvgEnergyLevel: energies.Count > 0 ? Math.Round(energies.Average(x => (decimal)x), 1) : null,
            LatestEnergyLevel: latest.EnergyLevel,
            DominantMood: moods.Count > 0
                ? moods.GroupBy(m => m).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key.ToString()
                : null,
            LatestMood: latest.Mood?.ToString(),
            TopSymptoms: topSymptoms,
            LatestSymptoms: CycleMapper.SymptomsToList(latest.Symptoms),
            LatestLogDate: latest.Date);
    }
}
