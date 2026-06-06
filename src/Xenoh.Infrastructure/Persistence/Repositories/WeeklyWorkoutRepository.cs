using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class WeeklyWorkoutRepository(ApplicationDbContext db) : IWeeklyWorkoutRepository
{
    public Task<bool> PlanAccessibleByUserAsync(Guid planId, Guid userId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .AnyAsync(p => p.Id == planId && (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);

    // Eager load: returns every week in the plan in one response (no pagination).
    // pageNumber/pageSize are accepted for API compatibility but ignored.
    public async Task<PagedResponse<WeeklyWorkoutResponse>> GetByPlanAsync(Guid planId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var items = await db.WeeklyWorkouts
          .AsNoTracking()
          .Where(w => w.PlanId == planId)
          .OrderBy(w => w.WeekNumber)
          .Select(w => new WeeklyWorkoutResponse(
              w.Id, w.WeekNumber, w.Name,
              w.StartDate, w.EndDate, w.PlanId,
              // TotalDays = effective days (within plan date range)
              w.DailyWorkouts.Count(d => d.Date >= w.Plan.StartDate && d.Date <= w.Plan.EndDate),
              // CompletedDays = effective days that are done / rest / missed
              w.DailyWorkouts.Count(d =>
                  d.Date >= w.Plan.StartDate && d.Date <= w.Plan.EndDate &&
                  (d.IsCompleted || d.Status == DayStatus.Rest || d.Status == DayStatus.Missed)),
              w.DailyWorkouts.Any(d => d.Exercises.Any(e => e.Sets.Any(s =>
                  s.IsCompleted &&
                  ((s.ActualReps != null && s.ActualReps < s.PlannedReps) ||
                   (s.ActualWeight != null && s.PlannedWeight != null && s.ActualWeight < s.PlannedWeight))))),
              w.IsCompleted,
              w.DailyWorkouts.Count(d => d.Date >= w.Plan.StartDate && d.Date <= w.Plan.EndDate)))
          .ToListAsync(ct);

        return new PagedResponse<WeeklyWorkoutResponse>(items, 1, items.Count, items.Count, false);
    }

    public Task<WeeklyWorkout?> FindForMutationAsync(Guid weekId, CancellationToken ct) =>
        db.WeeklyWorkouts
          .Include(w => w.DailyWorkouts)
              .ThenInclude(d => d.Exercises)
              .ThenInclude(e => e.Sets)
          .Include(w => w.Plan)
          .FirstOrDefaultAsync(w => w.Id == weekId, ct);

    public void RemoveRange(IEnumerable<WeeklyWorkout> weeks) =>
        db.WeeklyWorkouts.RemoveRange(weeks);

    public void AddRange(IEnumerable<WeeklyWorkout> weeks) =>
        db.WeeklyWorkouts.AddRange(weeks);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
