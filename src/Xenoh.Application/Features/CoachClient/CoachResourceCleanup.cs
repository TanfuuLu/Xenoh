using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.CoachClient;

/// <summary>
/// Tears down everything a coach authored for a client when their relationship ends.
/// <para>
/// Two separate paths end a relationship — <c>EndRelationshipHandler</c> and the
/// <c>Expired to Ended</c> transition in <c>ConnectByInviteCodeHandler</c> — and they
/// have to remove the same things, so the list lives here rather than in either one.
/// </para>
/// <para>
/// Everything is staged on the change tracker and left for the caller to commit, so the
/// cleanup lands in the same transaction as the relationship's own status change.
/// </para>
/// </summary>
internal static class CoachResourceCleanup
{
    public static async Task StageAsync(
        IApplicationDbContext db,
        IPlanRepository planRepo,
        ISupplementRepository supplementRepo,
        Guid clientId,
        Guid coachId,
        CancellationToken cancellationToken)
    {
        await planRepo.DeleteCoachPlansForClientAsync(clientId, coachId, cancellationToken);

        // The coach loses access the moment the relationship ends, so leaving these behind
        // would keep scheduling the client doses nobody can adjust.
        await supplementRepo.DeleteCoachRegimensForClientAsync(clientId, coachId, cancellationToken);

        await StageMealPlanDaysAsync(db, clientId, coachId, cancellationToken);
        await StageFileShareRevocationAsync(db, clientId, coachId, cancellationToken);
    }

    /// <summary>
    /// Only days the coach last wrote are removed. A day the client edited themselves has
    /// their id on it and stays, as do rows predating authorship tracking (null author).
    /// Checked items lose their plan row but not the resulting <c>FoodLog</c> — that FK is
    /// <c>SetNull</c>, so what the client actually ate survives.
    /// </summary>
    private static async Task StageMealPlanDaysAsync(
        IApplicationDbContext db,
        Guid clientId,
        Guid coachId,
        CancellationToken cancellationToken)
    {
        var days = await db.MealPlanDays
            .Where(d => d.UserId == clientId && d.CreatedByUserId == coachId)
            .ToListAsync(cancellationToken);

        db.MealPlanDays.RemoveRange(days);
    }

    /// <summary>
    /// Files the coach shared with the client stay downloadable through
    /// <c>StoredFileShare</c> rows regardless of relationship state, so they have to be
    /// revoked explicitly — otherwise an ex-client keeps access to the coach's documents.
    /// </summary>
    private static async Task StageFileShareRevocationAsync(
        IApplicationDbContext db,
        Guid clientId,
        Guid coachId,
        CancellationToken cancellationToken)
    {
        var shares = await db.StoredFileShares
            .Where(s =>
                (s.SharedByUserId == coachId && s.SharedWithUserId == clientId) ||
                (s.SharedByUserId == clientId && s.SharedWithUserId == coachId))
            .ToListAsync(cancellationToken);

        db.StoredFileShares.RemoveRange(shares);
    }
}
