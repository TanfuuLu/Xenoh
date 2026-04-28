using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class DailyWorkoutRepository(ApplicationDbContext db) : IDailyWorkoutRepository
{
    public Task<bool> WeekAccessibleByUserAsync(Guid weeklyWorkoutId, Guid userId, CancellationToken ct) =>
        db.WeeklyWorkouts
          .AsNoTracking()
          .Include(w => w.Plan)
          .AnyAsync(w => w.Id == weeklyWorkoutId &&
              (w.Plan.OwnerId == userId || w.Plan.CreatedByCoachId == userId), ct);

    public Task<List<DailyWorkoutResponse>> GetByWeekAsync(Guid weeklyWorkoutId, CancellationToken ct) =>
        db.DailyWorkouts
          .AsNoTracking()
          .Include(d => d.Exercises)
          .Where(d => d.WeeklyWorkoutId == weeklyWorkoutId)
          .OrderBy(d => d.Date)
          .Select(d => new DailyWorkoutResponse(
              d.Id, d.Date, d.DayOfWeek.ToString(),
              d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted), d.WeeklyWorkoutId,
              d.Exercises.Count,
              d.Exercises.Count(e => e.IsCompleted),
              d.Exercises.Any(e => e.Sets.Any(s =>
                  s.IsCompleted &&
                  ((s.ActualReps != null && s.ActualReps < s.PlannedReps) ||
                   (s.ActualWeight != null && s.PlannedWeight != null && s.ActualWeight < s.PlannedWeight))))))
          .ToListAsync(ct);

    public Task<DailyWorkout?> FindWithPlanAsync(Guid dailyWorkoutId, CancellationToken ct) =>
        db.DailyWorkouts
          .Include(d => d.WeeklyWorkout)
              .ThenInclude(w => w.Plan)
          .FirstOrDefaultAsync(d => d.Id == dailyWorkoutId, ct);

    public Task<DailyWorkout?> FindWithExercisesAndPlanAsync(Guid dailyWorkoutId, CancellationToken ct) =>
        db.DailyWorkouts
          .Include(d => d.WeeklyWorkout)
              .ThenInclude(w => w.Plan)
          .Include(d => d.Exercises)
              .ThenInclude(e => e.Sets)
          .FirstOrDefaultAsync(d => d.Id == dailyWorkoutId, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
