using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Cycle.Commands.DeleteCycleDailyLog;

public sealed class DeleteCycleDailyLogHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteCycleDailyLogCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteCycleDailyLogCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var log = await db.CycleDailyLogs
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == request.Date, cancellationToken);

        if (log is not null)
        {
            db.CycleDailyLogs.Remove(log);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
