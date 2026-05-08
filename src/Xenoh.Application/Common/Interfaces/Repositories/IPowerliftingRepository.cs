using Xenoh.Application.Common.Analytics;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IPowerliftingRepository
{
    /// <summary>True if the plan owned by <paramref name="ownerId"/> contains at least one
    /// completed or planned exercise whose template is flagged <c>IsCompetitionLift</c>.</summary>
    Task<bool> PlanHasCompetitionLiftsAsync(Guid planId, Guid ownerId, CancellationToken ct = default);

    /// <summary>Completed competition-lift sets for one user across one plan,
    /// projected for the analyzer.</summary>
    Task<IReadOnlyList<CompletedLiftSet>> GetCompletedSetsForPlanAsync(
        Guid planId, Guid ownerId, CompetitionLiftType lift, CancellationToken ct = default);

    /// <summary>Completed competition-lift sets for one user across all plans (longitudinal,
    /// used by the coach view).</summary>
    Task<IReadOnlyList<CompletedLiftSet>> GetCompletedSetsForUserAsync(
        Guid userId, CompetitionLiftType lift, CancellationToken ct = default);

    /// <summary>Bodyweight history for the user — used to build the DOTS-over-time series.</summary>
    Task<IReadOnlyList<BodyweightPoint>> GetBodyweightHistoryAsync(
        Guid userId, CancellationToken ct = default);
}
