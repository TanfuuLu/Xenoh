using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleLogs;

public sealed class GetCycleLogsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCycleLogsQuery, IReadOnlyList<CycleDailyLogResponse>>
{
    public async ValueTask<IReadOnlyList<CycleDailyLogResponse>> Handle(
        GetCycleLogsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        if (request.To < request.From)
            throw new InvalidOperationException("'to' date must be on or after 'from' date.");

        var logs = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= request.From && l.Date <= request.To)
            .OrderBy(l => l.Date)
            .ToListAsync(cancellationToken);

        return logs.Select(CycleMapper.ToResponse).ToList();
    }
}
