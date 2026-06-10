using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleOverview;

public sealed class GetCycleOverviewHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetCycleOverviewQuery, CycleOverviewResponse>
{
    public async ValueTask<CycleOverviewResponse> Handle(
        GetCycleOverviewQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prediction = await CyclePredictionLoader.LoadAsync(db, userId, today, cancellationToken);

        return CycleMapper.ToOverview(prediction);
    }
}
