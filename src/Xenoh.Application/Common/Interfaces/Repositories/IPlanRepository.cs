using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IPlanRepository
{
    /// <summary>Read-only: plans owned by user (with owner + coach + weeks + days).</summary>
    Task<List<PlanResponse>> GetAllByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Read-only: single plan visible to owner or coach, with full details.</summary>
    Task<PlanResponse?> GetByIdForUserAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Tracked: plan with weeks + days for mutation (activate, update, delete).</summary>
    Task<Plan?> FindForMutationAsync(Guid planId, CancellationToken ct = default);

    /// <summary>Tracked: plan that the caller owns or created (for delete).</summary>
    Task<Plan?> FindByIdAndCallerAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: client-owned plans created by the coach.</summary>
    Task<List<CoachPlanResponse>> GetCoachOverviewAsync(Guid coachId, CancellationToken ct = default);

    Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Read-only: analytics data for a plan (compliance, volume, muscle groups).</summary>
    Task<PlanAnalyticsResponse?> GetAnalyticsAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: for each owner, the total and completed day counts across all their plans.</summary>
    Task<List<(Guid OwnerId, int TotalDays, int CompletedDays)>> GetProgressByOwnersAsync(IEnumerable<Guid> ownerIds, CancellationToken ct = default);

    /// <summary>Read-only: active plan and adherence status for coach client monitoring.</summary>
    Task<List<CoachPlanMonitoringSnapshot>> GetMonitoringByOwnersAsync(IEnumerable<Guid> ownerIds, DateOnly today, CancellationToken ct = default);

    /// <summary>Bulk deactivate other active plans of the owner (ExecuteUpdate).</summary>
    Task DeactivateOthersAsync(Guid ownerId, Guid excludePlanId, CancellationToken ct = default);

    /// <summary>Delete all Coach-type plans owned by clientId and created by coachId.</summary>
    Task DeleteCoachPlansForClientAsync(Guid clientId, Guid coachId, CancellationToken ct = default);

    Task AddAsync(Plan plan, CancellationToken ct = default);
    void Remove(Plan plan);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
