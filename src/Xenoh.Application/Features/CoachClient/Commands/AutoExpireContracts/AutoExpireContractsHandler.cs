using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient.Agreements;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient.Commands.AutoExpireContracts;

public sealed class AutoExpireContractsHandler(
    IApplicationDbContext db, INotificationService notificationService,
    IPlanRepository plans, ISupplementRepository supplements
) : IRequestHandler<AutoExpireContractsCommand, int>
{
    public async ValueTask<int> Handle(AutoExpireContractsCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = CoachingPolicy.LocalDate(now);
        var due = await db.CoachClientRelationships
            .Where(r => r.Status != RelationshipStatus.Ended && r.Status != RelationshipStatus.Expired && r.Status != RelationshipStatus.Pending
                && ((r.EndDate != null && r.EndDate < today) || (r.NoticeEndsAtUtc != null && r.NoticeEndsAtUtc <= now)))
            .OrderBy(r => r.EndDate).Take(100).ToListAsync(ct);
        foreach (var r in due)
        {
            r.Status = r.NoticeEndsAtUtc != null ? RelationshipStatus.Ended : RelationshipStatus.Expired;
            r.EndedAtUtc = r.NoticeEndsAtUtc ?? CoachingPolicy.TermBoundary(r.EndDate!.Value);
            r.Revision = Guid.NewGuid();
            r.UpdatedAt = now;
            await CoachResourceCleanup.StageAsync(db, plans, supplements, r.ClientId, r.CoachId, ct);
            AgreementEvents.Record(db, r, r.Status.ToString(), null, r.EndedAtUtc);
        }
        await db.SaveChangesAsync(ct);

        // Reminders and start events are durable and emitted once per accepted version.
        var upcoming = await db.CoachClientRelationships.EffectiveAt(now)
            .Where(r => r.AgreementId != null && (!db.CoachingAgreementEvents.Any(e => e.AgreementId == r.AgreementId && e.Kind == "Started")
                || (r.EndDate <= today.AddDays(3) && !db.CoachingAgreementEvents.Any(e => e.AgreementId == r.AgreementId && e.Kind == "ExpiringSoon"))))
            .Take(100).ToListAsync(ct);
        var ids = upcoming.Select(r => r.AgreementId).ToList();
        var existing = await db.CoachingAgreementEvents.AsNoTracking().Where(e => ids.Contains(e.AgreementId)).ToListAsync(ct);
        foreach (var r in upcoming)
        {
            if (!existing.Any(e => e.AgreementId == r.AgreementId && e.Kind == "Started"))
                AgreementEvents.Record(db, r, "Started", null, CoachingPolicy.StartBoundary(r.StartDate));
            if (r.EndDate <= today.AddDays(3) && !existing.Any(e => e.AgreementId == r.AgreementId && e.Kind == "ExpiringSoon"))
                AgreementEvents.Record(db, r, "ExpiringSoon", null, CoachingPolicy.TermBoundary(r.EndDate!.Value));
            r.Revision = Guid.NewGuid();
        }
        await db.SaveChangesAsync(ct);
        await notificationService.DeliverPendingAgreementNotificationsAsync(ct);
        return due.Count;
    }
}
