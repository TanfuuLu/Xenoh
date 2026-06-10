using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Cycle.Queries.GetClientCycleOverview;

public sealed class GetClientCycleOverviewHandler(
    IApplicationDbContext db,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetClientCycleOverviewQuery, ClientCycleOverviewResponse>
{
    public async ValueTask<ClientCycleOverviewResponse> Handle(
        GetClientCycleOverviewQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        // 1. Must be an active coach-client relationship (else 403).
        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(coachId, request.ClientId, cancellationToken);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this client.");

        // 2. Client must be Female and have opted in to sharing (else 404 — tile hidden).
        var gender = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == request.ClientId)
            .Select(u => u.Gender)
            .FirstOrDefaultAsync(cancellationToken);

        if (gender != Gender.Female)
            throw new InvalidOperationException("Cycle data is not available for this client.");

        var shareWithCoach = await db.CycleSettings
            .AsNoTracking()
            .Where(s => s.UserId == request.ClientId)
            .Select(s => s.ShareWithCoach)
            .FirstOrDefaultAsync(cancellationToken);

        if (!shareWithCoach)
            throw new InvalidOperationException("This client has not shared their cycle data.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prediction = await CyclePredictionLoader.LoadAsync(db, request.ClientId, today, cancellationToken);

        // Frequent symptoms over the last 90 days (aggregate patterns only — no per-day detail).
        var since = today.AddDays(-90);
        var recentSymptoms = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == request.ClientId
                        && l.Date >= since && l.Date <= today
                        && l.Symptoms != CycleSymptoms.None)
            .Select(l => l.Symptoms)
            .ToListAsync(cancellationToken);

        var frequentSymptoms = CycleMapper.TopSymptoms(recentSymptoms);

        return CycleMapper.ToClientOverview(prediction, today, frequentSymptoms);
    }
}
