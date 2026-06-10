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
    IReadOnlyList<CycleSpan> PreMenstrualSpans);

/// <summary>
/// Builds an <see cref="AiCycleContext"/> for a user over a date window. Returns
/// <c>null</c> for non-female profiles so callers can omit cycle handling entirely.
/// Reuses the rule-based <see cref="CyclePhaseCalculator"/> + <see cref="CycleDayMarkerCalculator"/>.
/// </summary>
public static class CycleContextBuilder
{
    public const int PreMenstrualWindowDays = CycleDayMarkerCalculator.DefaultPreMenstrualWindowDays;

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
                PreMenstrualSpans: []);
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
                .Select(s => new CycleSpan(s.Start, s.End)).ToList());
    }
}
