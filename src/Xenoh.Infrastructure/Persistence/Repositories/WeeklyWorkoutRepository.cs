using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class WeeklyWorkoutRepository(ApplicationDbContext db) : IWeeklyWorkoutRepository
{
    public Task<bool> PlanAccessibleByUserAsync(Guid planId, Guid userId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .AnyAsync(p => p.Id == planId && (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);

    public Task<List<WeeklyWorkoutResponse>> GetByPlanAsync(Guid planId, CancellationToken ct) =>
        db.WeeklyWorkouts
          .AsNoTracking()
          .Include(w => w.DailyWorkouts)
          .Where(w => w.PlanId == planId)
          .OrderBy(w => w.WeekNumber)
          .Select(w => new WeeklyWorkoutResponse(
              w.Id, w.WeekNumber, w.Name,
              w.StartDate, w.EndDate, w.PlanId,
              w.DailyWorkouts.Count,
              w.DailyWorkouts.Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted))))
          .ToListAsync(ct);

    public Task<WeeklyWorkout?> FindForMutationAsync(Guid weekId, CancellationToken ct) =>
        db.WeeklyWorkouts
          .Include(w => w.DailyWorkouts)
          .Include(w => w.Plan)
          .FirstOrDefaultAsync(w => w.Id == weekId, ct);

    public void RemoveRange(IEnumerable<WeeklyWorkout> weeks) =>
        db.WeeklyWorkouts.RemoveRange(weeks);

    public void AddRange(IEnumerable<WeeklyWorkout> weeks) =>
        db.WeeklyWorkouts.AddRange(weeks);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
