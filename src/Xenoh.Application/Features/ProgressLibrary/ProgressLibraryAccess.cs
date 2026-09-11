using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ProgressLibrary;

internal static class ProgressLibraryAccess
{
    public static async Task EnsureCanCreateOrEditAsync(
        ISubscriptionService subscriptions,
        Guid userId,
        CancellationToken ct)
    {
        if (!await subscriptions.CanUseAdvancedAnalyticsAsync(userId, ct))
            throw new InvalidOperationException("An active paid subscription is required to save progress photos.");
    }

    public static async Task EnsureCoachCanViewAsync(
        IApplicationDbContext db,
        ISubscriptionService subscriptions,
        Guid coachId,
        Guid ownerId,
        CancellationToken ct)
    {
        var coachTier = await subscriptions.GetActiveTierAsync(coachId, ct);
        var ownerHasAccess = await subscriptions.CanUseAdvancedAnalyticsAsync(ownerId, ct);
        if (coachTier is not (PlanTier.ProCoach or PlanTier.Organizer) || !ownerHasAccess)
            throw new InvalidOperationException("Progress Library is unavailable for this coaching relationship.");

        var hasRelationship = await db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow)
            .AnyAsync(r => r.CoachId == coachId && r.ClientId == ownerId, ct);
        if (!hasRelationship)
            throw new InvalidOperationException("You do not have access to this athlete's progress photos.");
    }
}
