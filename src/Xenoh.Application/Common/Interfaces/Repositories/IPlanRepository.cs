using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Application.Features.Plans.Queries.GetPlanDesignAnalysis;
using Xenoh.Application.Features.Plans.Queries.ExportPlan;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IPlanRepository
{
    /// <summary>Read-only: plans owned by user (with owner + coach + weeks + days).</summary>
    Task<PagedResponse<PlanResponse>> GetAllByOwnerAsync(Guid ownerId, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>Read-only: single plan visible to owner or coach, with full details.</summary>
    Task<PlanResponse?> GetByIdForUserAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Tracked: plan with weeks + days for mutation (activate, update, delete).</summary>
    Task<Plan?> FindForMutationAsync(Guid planId, CancellationToken ct = default);

    /// <summary>Tracked: plan that the caller owns or created (for delete).</summary>
    Task<Plan?> FindByIdAndCallerAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: client-owned plans created by the coach.</summary>
    Task<PagedResponse<CoachPlanResponse>> GetCoachOverviewAsync(Guid coachId, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Read-only: analytics data for a plan (compliance, volume, muscle groups).</summary>
    Task<PlanAnalyticsResponse?> GetAnalyticsAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: planned-design analysis for a plan before execution.</summary>
    Task<PlanDesignAnalysisResponse?> GetDesignAnalysisAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: flat plan hierarchy (weeks → days → exercises) for CSV export. Returns null if the user has no access.</summary>
    Task<PlanExportData?> GetForExportAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: plan with full hierarchy (weeks → days → exercises → sets) for duplication. Returns null if user has no access.</summary>
    Task<Plan?> GetForDuplicateAsync(Guid planId, Guid userId, CancellationToken ct = default);

    /// <summary>Read-only: the coach-authored plan and adherence status for each client, for coach monitoring.</summary>
    Task<List<CoachPlanMonitoringSnapshot>> GetMonitoringByOwnersAsync(IEnumerable<Guid> ownerIds, Guid coachId, DateOnly today, CancellationToken ct = default);

    /// <summary>Bulk deactivate other active plans of the owner (ExecuteUpdate).</summary>
    Task DeactivateOthersAsync(Guid ownerId, Guid excludePlanId, CancellationToken ct = default);

    /// <summary>Delete all Coach-type plans owned by clientId and created by coachId.</summary>
    Task DeleteCoachPlansForClientAsync(Guid clientId, Guid coachId, CancellationToken ct = default);

    Task AddAsync(Plan plan, CancellationToken ct = default);
    void Remove(Plan plan);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
