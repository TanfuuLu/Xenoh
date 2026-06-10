using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Cycle.Common;

/// <summary>
/// Loads a user's recent flow logs + settings and runs the rule-based phase calculator.
/// </summary>
public static class CyclePredictionLoader
{
    public static async Task<CyclePrediction> LoadAsync(
        IApplicationDbContext db, Guid userId, DateOnly today, CancellationToken ct)
    {
        var since = today.AddDays(-400);

        var flowDays = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Flow != null && l.Date >= since && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Flow })
            .ToListAsync(ct);

        // Days the user logged but with NO flow ("normal" days). Used to detect an
        // early-ended period so the calendar re-predicts the remaining days.
        var noFlowDates = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Flow == null && l.Date >= since && l.Date <= today)
            .Select(l => l.Date)
            .ToListAsync(ct);

        var settings = await db.CycleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        var fd = flowDays.Select(x => new CycleFlowDay(x.Date, x.Flow!.Value));

        return CyclePhaseCalculator.Calculate(
            fd,
            settings?.AverageCycleLengthOverride,
            settings?.AveragePeriodLengthOverride,
            today,
            noFlowDates);
    }
}
