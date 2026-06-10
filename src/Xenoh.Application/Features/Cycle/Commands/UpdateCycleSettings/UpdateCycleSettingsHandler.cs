using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Cycle.Commands.UpdateCycleSettings;

public sealed class UpdateCycleSettingsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateCycleSettingsCommand, CycleSettingsResponse>
{
    public async ValueTask<CycleSettingsResponse> Handle(
        UpdateCycleSettingsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var settings = await db.CycleSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new CycleSettings { UserId = userId };
            db.CycleSettings.Add(settings);
        }

        settings.AverageCycleLengthOverride = request.AverageCycleLengthOverride;
        settings.AveragePeriodLengthOverride = request.AveragePeriodLengthOverride;
        settings.ShareWithCoach = request.ShareWithCoach;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new CycleSettingsResponse(
            settings.AverageCycleLengthOverride,
            settings.AveragePeriodLengthOverride,
            settings.ShareWithCoach);
    }
}
