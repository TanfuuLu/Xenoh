using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class DailyWorkoutRepository(ApplicationDbContext db) : IDailyWorkoutRepository
{
    public Task<bool> WeekAccessibleByUserAsync(Guid weeklyWorkoutId, Guid userId, CancellationToken ct) =>
        db.WeeklyWorkouts
          .AsNoTracking()
          .AnyAsync(w => w.Id == weeklyWorkoutId &&
              (w.Plan.OwnerId == userId || w.Plan.CreatedByCoachId == userId), ct);

    // Eager load: returns every day in the week in one response (no pagination).
    // pageNumber/pageSize are accepted for API compatibility but ignored.
    public async Task<PagedResponse<DailyWorkoutResponse>> GetByWeekAsync(Guid weeklyWorkoutId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var items = await db.DailyWorkouts
          .AsNoTracking()
          .Where(d => d.WeeklyWorkoutId == weeklyWorkoutId)
          .OrderBy(d => d.Date)
          .Select(d => new DailyWorkoutResponse(
              d.Id, d.Date, d.DayOfWeek.ToString(),
              d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted || e.IsSkipped), d.WeeklyWorkoutId,
              d.Exercises.Count,
              d.Exercises.Count(e => e.IsCompleted),
              d.Exercises.Count(e => e.IsSkipped),
              d.Exercises.Any(e => e.Sets.Any(s =>
                  s.IsCompleted &&
                  ((s.ActualReps != null && s.ActualReps < s.PlannedReps) ||
                   (s.ActualWeight != null && s.PlannedWeight != null && s.ActualWeight < s.PlannedWeight)))),
              d.Status.ToString()))
          .ToListAsync(ct);

        return new PagedResponse<DailyWorkoutResponse>(items, 1, items.Count, items.Count, false);
    }

    public Task<DailyWorkout?> FindWithPlanAsync(Guid dailyWorkoutId, CancellationToken ct) =>
        db.DailyWorkouts
          .Include(d => d.WeeklyWorkout)
              .ThenInclude(w => w.DailyWorkouts)
          .Include(d => d.WeeklyWorkout)
              .ThenInclude(w => w.Plan)
          .FirstOrDefaultAsync(d => d.Id == dailyWorkoutId, ct);

    public Task<DailyWorkout?> FindWithExercisesAndPlanAsync(Guid dailyWorkoutId, CancellationToken ct) =>
        db.DailyWorkouts
          .Include(d => d.WeeklyWorkout)
              .ThenInclude(w => w.Plan)
          .Include(d => d.Exercises)
              .ThenInclude(e => e.Sets)
          // Two nested collections (exercises x sets) — split avoids a cartesian JOIN.
          .AsSplitQuery()
          .FirstOrDefaultAsync(d => d.Id == dailyWorkoutId, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
