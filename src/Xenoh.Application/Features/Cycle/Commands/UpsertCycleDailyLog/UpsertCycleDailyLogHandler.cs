using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Cycle.Commands.UpsertCycleDailyLog;

public sealed class UpsertCycleDailyLogHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UpsertCycleDailyLogCommand, CycleDailyLogResponse>
{
    public async ValueTask<CycleDailyLogResponse> Handle(
        UpsertCycleDailyLogCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var log = await db.CycleDailyLogs
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == request.Date, cancellationToken);

        if (log is null)
        {
            log = new CycleDailyLog { UserId = userId, Date = request.Date };
            db.CycleDailyLogs.Add(log);
        }

        log.Flow = request.Flow;
        log.Symptoms = CycleMapper.ListToSymptoms(request.Symptoms);
        log.Mood = request.Mood;
        log.EnergyLevel = request.EnergyLevel;
        log.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        log.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return CycleMapper.ToResponse(log);
    }
}
