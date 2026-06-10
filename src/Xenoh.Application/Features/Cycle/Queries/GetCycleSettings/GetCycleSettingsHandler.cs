using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleSettings;

public sealed class GetCycleSettingsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCycleSettingsQuery, CycleSettingsResponse>
{
    public async ValueTask<CycleSettingsResponse> Handle(
        GetCycleSettingsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var settings = await db.CycleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        return new CycleSettingsResponse(
            settings?.AverageCycleLengthOverride,
            settings?.AveragePeriodLengthOverride,
            settings?.ShareWithCoach ?? false);
    }
}
