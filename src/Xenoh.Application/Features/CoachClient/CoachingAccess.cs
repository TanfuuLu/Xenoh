using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.CoachClient;

public static class CoachingAccess
{
    // SQL-translatable; evaluate the clock once per authorization query.
    public static IQueryable<CoachClientRelationship> EffectiveAt(
        this IQueryable<CoachClientRelationship> query, DateTime now)
    {
        var today = CoachingPolicy.LocalDate(now);
        return query.Where(r =>
            (r.Status == RelationshipStatus.Active || r.Status == RelationshipStatus.PendingTermination || r.Status == RelationshipStatus.PendingRenewal)
            && r.StartDate <= today && (r.EndDate == null || r.EndDate >= today)
            && (r.NoticeEndsAtUtc == null || r.NoticeEndsAtUtc > now));
    }

    public static IQueryable<Plan> WritableCoachingPlans(this IQueryable<Plan> query, IApplicationDbContext db)
    {
        var active = db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow);
        return query.Where(p => !p.IsCoachingArchived &&
            (p.PlanType != PlanType.Coach || active.Any(r => r.ClientId == p.OwnerId && r.CoachId == p.CreatedByCoachId
                && (p.CoachingRelationshipId == null || p.CoachingRelationshipId == r.Id))));
    }

    public static IQueryable<Plan> AccessibleTo(this IQueryable<Plan> query, IApplicationDbContext db, Guid userId)
    {
        var writable = db.Plans.WritableCoachingPlans(db);
        return query.Where(p => p.OwnerId == userId ||
            (p.CreatedByCoachId == userId && writable.Any(a => a.Id == p.Id)));
    }
}
