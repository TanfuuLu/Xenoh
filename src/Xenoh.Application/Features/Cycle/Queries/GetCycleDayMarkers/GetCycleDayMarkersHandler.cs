using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleDayMarkers;

public sealed class GetCycleDayMarkersHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCycleDayMarkersQuery, CycleDayMarkersResponse>
{
    // Guard rail: a plan can span many months, but reject absurd ranges.
    private const int MaxRangeDays = 400;

    public async ValueTask<CycleDayMarkersResponse> Handle(
        GetCycleDayMarkersQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        if (request.To < request.From)
            throw new InvalidOperationException("'to' must be on or after 'from'.");
        if (request.To.DayNumber - request.From.DayNumber > MaxRangeDays)
            throw new InvalidOperationException($"Date range cannot exceed {MaxRangeDays} days.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-400);

        var flowRows = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Flow != null && l.Date >= since && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Flow })
            .ToListAsync(cancellationToken);

        var settings = await db.CycleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var flowDays = flowRows.Select(x => new CycleFlowDay(x.Date, x.Flow!.Value)).ToList();

        var prediction = CyclePhaseCalculator.Calculate(
            flowDays,
            settings?.AverageCycleLengthOverride,
            settings?.AveragePeriodLengthOverride,
            today);

        if (prediction.NeedsData || prediction.LastPeriodStart is null)
        {
            return new CycleDayMarkersResponse(
                request.From,
                request.To,
                CycleDayMarkerCalculator.DefaultPreMenstrualWindowDays,
                NeedsData: true,
                Days: []);
        }

        var marks = CycleDayMarkerCalculator.Calculate(
            prediction, flowDays, request.From, request.To);

        var days = marks
            .Select(m => new CycleDayMarkerResponse(m.Date, m.Marker.ToString()))
            .ToList();

        return new CycleDayMarkersResponse(
            request.From,
            request.To,
            CycleDayMarkerCalculator.DefaultPreMenstrualWindowDays,
            NeedsData: false,
            Days: days);
    }
}
