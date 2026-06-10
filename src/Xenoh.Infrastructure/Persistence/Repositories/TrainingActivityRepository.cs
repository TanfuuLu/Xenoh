using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class TrainingActivityRepository(ApplicationDbContext db) : ITrainingActivityRepository
{
    public async Task<TrainingActivitySnapshot> GetActivityAsync(
        Guid userId,
        DateOnly accountCreatedDate,
        DateOnly today,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var totalDurationSeconds = await db.Exercises
            .AsNoTracking()
            .Where(e =>
                e.DurationSeconds.HasValue &&
                e.DurationSeconds.Value > 0 &&
                e.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                e.DailyWorkout.Date >= accountCreatedDate &&
                e.DailyWorkout.Date <= today)
            .SumAsync(e => (long?)e.DurationSeconds!.Value, ct) ?? 0L;

        var totalWeightTrainedKg = await db.ExerciseSets
            .AsNoTracking()
            .Where(s =>
                s.IsCompleted &&
                (s.ActualReps ?? s.PlannedReps) > 0 &&
                (s.ActualWeight ?? s.PlannedWeight) > 0m &&
                s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                s.Exercise.DailyWorkout.Date >= accountCreatedDate &&
                s.Exercise.DailyWorkout.Date <= today)
            .SumAsync(s => (decimal?)((s.ActualReps ?? s.PlannedReps) *
                                      (s.ActualWeight ?? s.PlannedWeight ?? 0m)), ct) ?? 0m;

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var effectiveStart = monthStart < accountCreatedDate ? accountCreatedDate : monthStart;
        var effectiveEnd = monthEnd > today ? today : monthEnd;

        if (effectiveStart > effectiveEnd)
            return new TrainingActivitySnapshot(totalDurationSeconds, totalWeightTrainedKg, []);

        var historyDates = await db.WorkoutHistories
            .AsNoTracking()
            .Where(h =>
                h.UserId == userId &&
                h.Date >= effectiveStart &&
                h.Date <= effectiveEnd)
            .Select(h => h.Date)
            .ToListAsync(ct);

        var durationDates = await db.Exercises
            .AsNoTracking()
            .Where(e =>
                e.DurationSeconds.HasValue &&
                e.DurationSeconds.Value > 0 &&
                e.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                e.DailyWorkout.Date >= effectiveStart &&
                e.DailyWorkout.Date <= effectiveEnd)
            .Select(e => e.DailyWorkout.Date)
            .Distinct()
            .ToListAsync(ct);

        var trainedDates = historyDates
            .Concat(durationDates)
            .Distinct()
            .Order()
            .ToList();

        return new TrainingActivitySnapshot(totalDurationSeconds, totalWeightTrainedKg, trainedDates);
    }

    public async Task<List<MonthlyVolumeBucket>> GetMonthlyVolumeAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly today,
        CancellationToken ct = default)
    {
        return await db.ExerciseSets
            .AsNoTracking()
            .Where(s =>
                s.IsCompleted &&
                (s.ActualReps ?? s.PlannedReps) > 0 &&
                (s.ActualWeight ?? s.PlannedWeight) > 0m &&
                s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                s.Exercise.DailyWorkout.Date >= fromDate &&
                s.Exercise.DailyWorkout.Date <= today)
            .GroupBy(s => new { s.Exercise.DailyWorkout.Date.Year, s.Exercise.DailyWorkout.Date.Month })
            .Select(g => new MonthlyVolumeBucket(
                g.Key.Year,
                g.Key.Month,
                g.Sum(s => (s.ActualReps ?? s.PlannedReps) * (s.ActualWeight ?? s.PlannedWeight ?? 0m))))
            .ToListAsync(ct);
    }
}
