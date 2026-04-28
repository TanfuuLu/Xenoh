using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class ExerciseRepository(ApplicationDbContext db) : IExerciseRepository
{
    public async Task<List<ExerciseResponse>> GetByDayWithPrsAsync(
        Guid dailyWorkoutId, Guid userId, CancellationToken ct)
    {
        var dayAccessible = await db.DailyWorkouts
            .AsNoTracking()
            .Include(d => d.WeeklyWorkout).ThenInclude(w => w.Plan)
            .AnyAsync(d => d.Id == dailyWorkoutId &&
                (d.WeeklyWorkout.Plan.OwnerId == userId ||
                 d.WeeklyWorkout.Plan.CreatedByCoachId == userId), ct);

        if (!dayAccessible)
            throw new InvalidOperationException("Daily workout not found.");

        var exercises = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Sets)
            .Where(e => e.DailyWorkoutId == dailyWorkoutId)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var templateIds = exercises.Select(e => e.ExerciseTemplateId).Distinct().ToList();

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId && templateIds.Contains(p.ExerciseTemplateId))
            .ToDictionaryAsync(p => p.ExerciseTemplateId, p => (decimal?)p.Weight, ct);

        return exercises
            .Select(e => CreateExerciseHandler.ToResponse(e, prs.GetValueOrDefault(e.ExerciseTemplateId)))
            .ToList();
    }

    public Task<Exercise?> FindWithSetsAndPlanAsync(Guid exerciseId, CancellationToken ct) =>
        db.Exercises
          .Include(e => e.Sets)
          .Include(e => e.DailyWorkout)
              .ThenInclude(d => d.WeeklyWorkout)
                  .ThenInclude(w => w.Plan)
          .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

    public Task<Exercise?> FindWithPlanAsync(Guid exerciseId, CancellationToken ct) =>
        db.Exercises
          .Include(e => e.DailyWorkout)
              .ThenInclude(d => d.Exercises)
          .Include(e => e.DailyWorkout)
              .ThenInclude(d => d.WeeklyWorkout)
                  .ThenInclude(w => w.Plan)
          .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

    public async Task<int> GetNextSortOrderAsync(Guid dailyWorkoutId, CancellationToken ct = default)
    {
        var maxSortOrder = await db.Exercises
            .Where(e => e.DailyWorkoutId == dailyWorkoutId)
            .Select(e => (int?)e.SortOrder)
            .MaxAsync(ct);

        return (maxSortOrder ?? -1) + 1;
    }

    public Task<List<Exercise>> GetByIdsWithPlanAsync(IEnumerable<Guid> exerciseIds, CancellationToken ct = default)
    {
        var ids = exerciseIds.ToList();

        return db.Exercises
            .Include(e => e.DailyWorkout)
                .ThenInclude(d => d.WeeklyWorkout)
                    .ThenInclude(w => w.Plan)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Exercise exercise, CancellationToken ct) =>
        await db.Exercises.AddAsync(exercise, ct);

    public void Remove(Exercise exercise) => db.Exercises.Remove(exercise);

    public void RemoveRange(IEnumerable<Exercise> exercises) =>
        db.Exercises.RemoveRange(exercises);

    public void AddRange(IEnumerable<Exercise> exercises) =>
        db.Exercises.AddRange(exercises);

    public void RemoveSetRange(IEnumerable<ExerciseSet> sets) =>
        db.ExerciseSets.RemoveRange(sets);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
