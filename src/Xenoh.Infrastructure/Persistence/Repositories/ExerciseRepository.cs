using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class ExerciseRepository(ApplicationDbContext db) : IExerciseRepository
{
    // Eager load: returns every exercise for the day in one response (no pagination).
    // pageNumber/pageSize are accepted for API compatibility but ignored.
    public async Task<PagedResponse<ExerciseResponse>> GetByDayWithPrsAsync(
        Guid dailyWorkoutId, Guid userId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var day = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d => d.Id == dailyWorkoutId &&
                (d.WeeklyWorkout.Plan.OwnerId == userId ||
                 d.WeeklyWorkout.Plan.CreatedByCoachId == userId))
            .Select(d => new
            {
                d.Date,
                d.WeeklyWorkout.Plan.OwnerId
            })
            .FirstOrDefaultAsync(ct);

        if (day is null)
            throw new InvalidOperationException("Daily workout not found.");

        var exercises = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Sets)
            .Include(e => e.ExerciseTemplate)
            .Where(e => e.DailyWorkoutId == dailyWorkoutId)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var templateIds = exercises.Select(e => e.ExerciseTemplateId).Distinct().ToList();

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId && templateIds.Contains(p.ExerciseTemplateId))
            .ToDictionaryAsync(p => p.ExerciseTemplateId, p => (decimal?)p.Weight, ct);

        var bodyweight = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == day.OwnerId && b.Date <= day.Date)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(ct);

        var items = exercises
            .Select(e => CreateExerciseHandler.ToResponse(
                e,
                prs.GetValueOrDefault(e.ExerciseTemplateId),
                bodyweight))
            .ToList();

        return new PagedResponse<ExerciseResponse>(items, 1, items.Count, items.Count, false);
    }

    // One request for the whole week instead of one-per-day (N+1). Runs a fixed
    // number of bounded queries regardless of how many days the week has:
    // week+days, exercises (with sets/template), PRs, and bodyweight logs.
    public async Task<List<ExerciseResponse>> GetByWeekWithPrsAsync(
        Guid weeklyWorkoutId, Guid userId, CancellationToken ct = default)
    {
        var week = await db.WeeklyWorkouts
            .AsNoTracking()
            .Where(w => w.Id == weeklyWorkoutId &&
                (w.Plan.OwnerId == userId || w.Plan.CreatedByCoachId == userId))
            .Select(w => new
            {
                w.Plan.OwnerId,
                Days = w.DailyWorkouts.Select(d => new { d.Id, d.Date }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (week is null)
            throw new InvalidOperationException("Week not found.");

        var exercises = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Sets)
            .Include(e => e.ExerciseTemplate)
            .Where(e => e.DailyWorkout.WeeklyWorkoutId == weeklyWorkoutId)
            .OrderBy(e => e.DailyWorkout.Date)
            .ThenBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(ct);

        if (exercises.Count == 0)
            return [];

        var templateIds = exercises.Select(e => e.ExerciseTemplateId).Distinct().ToList();

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId && templateIds.Contains(p.ExerciseTemplateId))
            .ToDictionaryAsync(p => p.ExerciseTemplateId, p => (decimal?)p.Weight, ct);

        // Bodyweight for a day = latest log on or before that day's date.
        // Load the owner's logs once, then resolve each day in memory.
        var maxDate = week.Days.Max(d => d.Date);
        var bodyweightLogs = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == week.OwnerId && b.Date <= maxDate)
            .OrderByDescending(b => b.Date)
            .Select(b => new { b.Date, b.Weight })
            .ToListAsync(ct);

        var bodyweightByDay = week.Days.ToDictionary(
            d => d.Id,
            d => bodyweightLogs.FirstOrDefault(b => b.Date <= d.Date)?.Weight);

        return exercises
            .Select(e => CreateExerciseHandler.ToResponse(
                e,
                prs.GetValueOrDefault(e.ExerciseTemplateId),
                bodyweightByDay.GetValueOrDefault(e.DailyWorkoutId)))
            .ToList();
    }

    public Task<Exercise?> FindWithSetsAndPlanAsync(Guid exerciseId, CancellationToken ct) =>
        db.Exercises
          .Include(e => e.Sets)
          .Include(e => e.ExerciseTemplate)
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
            .AsNoTracking()
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

    public void AddSetRange(IEnumerable<ExerciseSet> sets) =>
        db.ExerciseSets.AddRange(sets);

    public void RemoveSetRange(IEnumerable<ExerciseSet> sets) =>
        db.ExerciseSets.RemoveRange(sets);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
