using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class PlanRepository(ApplicationDbContext db) : IPlanRepository
{
    public Task<List<PlanResponse>> GetAllByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .Where(p => p.OwnerId == ownerId)
          .Select(p => new PlanResponse(
              p.Id, p.Name, p.StartDate, p.EndDate,
              p.PlanType.ToString(), p.OwnerId,
              (p.Owner.FirstName + " " + p.Owner.LastName).Trim(),
              p.CreatedByCoachId,
              p.CreatedByCoach == null ? null : (p.CreatedByCoach.FirstName + " " + p.CreatedByCoach.LastName).Trim(),
              p.WeeklyWorkouts.Count,
              p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count),
              p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count(d => d.IsCompleted)),
              p.IsActive, p.CreatedAt))
          .ToListAsync(ct);

    public async Task<PlanResponse?> GetByIdForUserAsync(Guid planId, Guid userId, CancellationToken ct)
    {
        var plan = await db.Plans
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.CreatedByCoach)
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstOrDefaultAsync(p => p.Id == planId &&
                (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);

        if (plan is null) return null;

        var allDays = plan.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).ToList();
        return ToPlanResponse(plan, allDays);
    }

    public Task<Plan?> FindForMutationAsync(Guid planId, CancellationToken ct) =>
        db.Plans
          .Include(p => p.Owner)
          .Include(p => p.CreatedByCoach)
          .Include(p => p.WeeklyWorkouts)
              .ThenInclude(w => w.DailyWorkouts)
          .FirstOrDefaultAsync(p => p.Id == planId, ct);

    public Task<Plan?> FindByIdAndCallerAsync(Guid planId, Guid userId, CancellationToken ct) =>
        db.Plans
          .FirstOrDefaultAsync(p => p.Id == planId &&
              (p.OwnerId == userId ||
               (p.PlanType == PlanType.Coach && p.CreatedByCoachId == userId)), ct);

    public Task<List<CoachPlanResponse>> GetCoachOverviewAsync(Guid coachId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .Include(p => p.WeeklyWorkouts)
          .Include(p => p.Owner)
          .Where(p => (p.OwnerId == coachId && p.PlanType == PlanType.Self) || p.CreatedByCoachId == coachId)
          .OrderByDescending(p => p.CreatedAt)
          .Select(p => new CoachPlanResponse(
              p.Id, p.Name, p.StartDate, p.EndDate,
              p.PlanType.ToString(), p.OwnerId,
              $"{p.Owner.FirstName} {p.Owner.LastName}",
              p.Owner.Email!,
              p.WeeklyWorkouts.Count,
              p.CreatedAt))
          .ToListAsync(ct);

    public Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        db.Plans.CountAsync(p => p.OwnerId == ownerId, ct);

    public async Task<List<(Guid OwnerId, int TotalDays, int CompletedDays)>> GetProgressByOwnersAsync(
        IEnumerable<Guid> ownerIds, CancellationToken ct)
    {
        var ids = ownerIds.ToList();
        var rows = await db.Plans
            .AsNoTracking()
            .Where(p => ids.Contains(p.OwnerId))
            .Select(p => new
            {
                p.OwnerId,
                TotalDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(),
                CompletedDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.IsCompleted)
            })
            .ToListAsync(ct);

        return rows.Select(r => (r.OwnerId, r.TotalDays, r.CompletedDays)).ToList();
    }

    public Task DeactivateOthersAsync(Guid ownerId, Guid excludePlanId, CancellationToken ct) =>
        db.Plans
          .Where(p => p.OwnerId == ownerId && p.IsActive && p.Id != excludePlanId)
          .ExecuteUpdateAsync(s => s
              .SetProperty(p => p.IsActive, false)
              .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

    public async Task DeleteCoachPlansForClientAsync(Guid clientId, Guid coachId, CancellationToken ct)
    {
        var plans = await db.Plans
            .Where(p => p.OwnerId == clientId &&
                        p.CreatedByCoachId == coachId &&
                        p.PlanType == PlanType.Coach)
            .ToListAsync(ct);

        db.Plans.RemoveRange(plans);
    }

    public async Task AddAsync(Plan plan, CancellationToken ct) =>
        await db.Plans.AddAsync(plan, ct);

    public void Remove(Plan plan) => db.Plans.Remove(plan);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    // ── helpers ──────────────────────────────────────────────────────────────

    internal static PlanResponse ToPlanResponse(Plan p, List<DailyWorkout> allDays) => new(
        p.Id, p.Name, p.StartDate, p.EndDate,
        p.PlanType.ToString(), p.OwnerId,
        $"{p.Owner.FirstName} {p.Owner.LastName}".Trim(),
        p.CreatedByCoachId,
        p.CreatedByCoach is null ? null : $"{p.CreatedByCoach.FirstName} {p.CreatedByCoach.LastName}".Trim(),
        p.WeeklyWorkouts.Count,
        allDays.Count,
        allDays.Count(d => d.IsCompleted),
        p.IsActive, p.CreatedAt);
}
