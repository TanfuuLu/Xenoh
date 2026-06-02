using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Application.Features.Plans.Queries.GetPlanDesignAnalysis;
using Xenoh.Application.Features.Plans.Queries.ExportPlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class PlanRepository(ApplicationDbContext db) : IPlanRepository
{
    // Single source of truth for the Plan -> PlanResponse SQL projection,
    // shared by every list/detail query so the shape can never drift.
    private static readonly Expression<Func<Plan, PlanResponse>> ToResponse = p => new PlanResponse(
        p.Id, p.Name, p.StartDate, p.EndDate,
        p.PlanType.ToString(), p.OwnerId,
        (p.Owner.FirstName + " " + p.Owner.LastName).Trim(),
        p.CreatedByCoachId,
        p.CreatedByCoach == null ? null : (p.CreatedByCoach.FirstName + " " + p.CreatedByCoach.LastName).Trim(),
        p.WeeklyWorkouts.Count,
        p.WeeklyWorkouts.Count(w => w.IsCompleted),
        p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count),
        p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted))),
        p.IsActive, p.CreatedAt);

    public async Task<PagedResponse<PlanResponse>> GetAllByOwnerAsync(Guid ownerId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = db.Plans
          .AsNoTracking()
          .Where(p => p.OwnerId == ownerId)
          .OrderByDescending(p => p.IsActive)
          .ThenByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .Select(ToResponse)
          .ToListAsync(ct);

        return new PagedResponse<PlanResponse>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            pageNumber * pageSize < totalCount);
    }

    public async Task<PlanResponse?> GetByIdForUserAsync(Guid planId, Guid userId, CancellationToken ct)
    {
        return await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == planId && (p.OwnerId == userId || p.CreatedByCoachId == userId))
            .Select(ToResponse)
            .FirstOrDefaultAsync(ct);
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

    public async Task<PagedResponse<CoachPlanResponse>> GetCoachOverviewAsync(Guid coachId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = db.Plans
          .AsNoTracking()
          .Where(p =>
              p.PlanType == PlanType.Coach &&
              p.CreatedByCoachId == coachId &&
              p.OwnerId != coachId)
          .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .Select(p => new CoachPlanResponse(
              p.Id, p.Name, p.StartDate, p.EndDate,
              p.PlanType.ToString(), p.OwnerId,
              $"{p.Owner.FirstName} {p.Owner.LastName}",
              p.Owner.Email!,
              p.WeeklyWorkouts.Count,
              p.CreatedAt))
          .ToListAsync(ct);

        return new PagedResponse<CoachPlanResponse>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            pageNumber * pageSize < totalCount);
    }

    public Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .CountAsync(p => p.OwnerId == ownerId, ct);

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
                TotalDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.Status != DayStatus.Rest),
                CompletedDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted))
            })
            .ToListAsync(ct);

        return rows.Select(r => (r.OwnerId, r.TotalDays, r.CompletedDays)).ToList();
    }

    public async Task<List<CoachPlanMonitoringSnapshot>> GetMonitoringByOwnersAsync(
        IEnumerable<Guid> ownerIds,
        DateOnly today,
        CancellationToken ct)
    {
        var ids = ownerIds.ToList();
        if (ids.Count == 0)
            return [];

        var plans = await db.Plans
            .AsNoTracking()
            .Where(p => ids.Contains(p.OwnerId) &&
                p.StartDate <= today &&
                p.EndDate >= today)
            .Select(p => new
            {
                p.Id,
                p.OwnerId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.CreatedAt,
                Days = p.WeeklyWorkouts
                    .SelectMany(w => w.DailyWorkouts)
                    .Where(d => d.Status != DayStatus.Rest)
                    .Select(d => new
                    {
                        d.Date,
                        d.Status,
                        IsCompleted = d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return plans
            .GroupBy(p => p.OwnerId)
            .Select(g =>
            {
                var plan = g
                    .OrderByDescending(p => p.IsActive)
                    .ThenByDescending(p => p.StartDate)
                    .ThenByDescending(p => p.CreatedAt)
                    .First();

                var totalDays = plan.Days.Count;
                var completedDays = plan.Days.Count(d => d.IsCompleted);
                var missedDays = plan.Days.Count(d => d.Status == DayStatus.Missed);
                var elapsedDays = plan.Days.Count(d => d.Date <= today);
                var progress = totalDays == 0
                    ? 0
                    : (int)Math.Round(completedDays * 100.0 / totalDays);
                var expectedProgress = totalDays == 0
                    ? 0
                    : (int)Math.Round(elapsedDays * 100.0 / totalDays);

                return new CoachPlanMonitoringSnapshot(
                    plan.OwnerId,
                    plan.Id,
                    plan.Name,
                    plan.StartDate,
                    plan.EndDate,
                    progress,
                    missedDays,
                    completedDays,
                    totalDays,
                    expectedProgress);
            })
            .ToList();
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

    public async Task<PlanDesignAnalysisResponse?> GetDesignAnalysisAsync(Guid planId, Guid userId, CancellationToken ct)
    {
        var canAccess = await db.Plans.AsNoTracking()
            .AnyAsync(p => p.Id == planId &&
                (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);
        if (!canAccess) return null;

        var weeks = await db.WeeklyWorkouts
            .AsNoTracking()
            .Where(w => w.PlanId == planId)
            .OrderBy(w => w.WeekNumber)
            .Select(w => new
            {
                w.WeekNumber,
                w.Name,
                Days = w.DailyWorkouts
                    .OrderBy(d => d.Date)
                    .Select(d => new PlannedDaySnapshot(
                        d.Date,
                        d.Status,
                        d.Exercises
                            .OrderBy(e => e.SortOrder)
                            .Select(e => new PlannedExerciseSnapshot(
                                e.Name,
                                e.PrimaryMuscleGroup,
                                e.SecondaryMuscleGroups,
                                Math.Max(0, e.PlannedSets),
                                Math.Max(0, e.PlannedReps),
                                e.PlannedWeight))
                            .ToList()))
                    .ToList()
            })
            .ToListAsync(ct);

        var days = weeks
            .SelectMany(w => w.Days.Select(d => d with { WeekNumber = w.WeekNumber, WeekName = w.Name }))
            .OrderBy(d => d.Date)
            .ToList();
        var trainingDays = days.Where(IsPlannedTrainingDay).ToList();
        var exercises = trainingDays.SelectMany(d => d.Exercises).ToList();
        var plannedSets = exercises.Sum(e => e.PlannedSets);
        var plannedRepVolume = exercises.Sum(e => e.PlannedSets * e.PlannedReps);
        var plannedTonnage = exercises.Sum(e => e.PlannedWeight is null
            ? 0m
            : e.PlannedSets * e.PlannedReps * e.PlannedWeight.Value);

        var structure = new PlanStructureResponse(
            weeks.Count,
            trainingDays.Count,
            days.Count(d => d.Status == DayStatus.Rest),
            weeks.Count == 0 ? 0m : Math.Round((decimal)trainingDays.Count / weeks.Count, 1),
            LongestTrainingStreak(days));

        var workload = new PlanWorkloadResponse(
            exercises.Count,
            plannedSets,
            plannedRepVolume,
            plannedTonnage,
            trainingDays.Count == 0 ? 0m : Math.Round((decimal)exercises.Count / trainingDays.Count, 1));

        var muscleEntries = exercises.SelectMany(BuildPlannedMuscleEntries).ToList();
        var totalWeightedSets = muscleEntries.Sum(e => e.WeightedSets);
        var muscleGroups = muscleEntries
            .GroupBy(e => e.MuscleGroup)
            .Select(g =>
            {
                var total = g.Sum(e => e.WeightedSets);
                var percent = totalWeightedSets == 0m ? 0m : Math.Round(total / totalWeightedSets * 100m, 1);
                return new PlannedMuscleGroupPoint(
                    g.Key.ToString(),
                    total,
                    g.Sum(e => e.PrimarySets),
                    g.Sum(e => e.SecondarySets),
                    percent,
                    MuscleStatus(percent));
            })
            .OrderByDescending(m => m.WeightedSets)
            .ThenBy(m => m.MuscleGroup)
            .ToList();

        var balance = BuildPlanDesignBalance(muscleEntries, muscleGroups);
        var movementPatterns = BuildMovementPatterns(exercises);
        var recoveryRisks = BuildRecoveryRisks(days, trainingDays);
        var repeatedExercises = exercises
            .GroupBy(e => e.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => new RepeatedExercisePoint(g.Key, g.Count()))
            .Take(5)
            .ToList();
        var variety = new PlanVarietyResponse(
            exercises.Select(e => e.Name.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            repeatedExercises.Count,
            repeatedExercises);

        return new PlanDesignAnalysisResponse(
            structure,
            workload,
            muscleGroups,
            balance,
            movementPatterns,
            recoveryRisks,
            variety);
    }

    public async Task<PlanAnalyticsResponse?> GetAnalyticsAsync(Guid planId, Guid userId, CancellationToken ct)
    {
        var canAccess = await db.Plans.AsNoTracking()
            .AnyAsync(p => p.Id == planId &&
                (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);
        if (!canAccess) return null;

        var weeks = await db.WeeklyWorkouts.AsNoTracking()
            .Where(w => w.PlanId == planId)
            .OrderBy(w => w.WeekNumber)
            .Select(w => new
            {
                w.WeekNumber,
                w.Name,
                Days = w.DailyWorkouts.Select(d => new
                {
                    d.IsCompleted,
                    d.Status,
                    IsRest = d.Status == DayStatus.Rest,
                    IsMissed = d.Status == DayStatus.Missed,
                    HasWarning = d.Exercises.Any(e => e.Sets.Any(s =>
                        s.IsCompleted &&
                        ((s.ActualReps != null && s.ActualReps < s.PlannedReps) ||
                         (s.ActualWeight != null && s.PlannedWeight != null && s.ActualWeight < s.PlannedWeight)))),
                    CompletedExerciseDurations = d.Exercises
                        .Where(e => e.IsCompleted && e.DurationSeconds != null)
                        .Select(e => e.DurationSeconds!.Value)
                        .ToList(),
                    Sets = d.Exercises.SelectMany(e => e.Sets
                        .Where(s => s.IsCompleted)
                        .Select(s => new
                        {
                            s.ActualReps,
                            s.ActualWeight,
                            s.Rpe,
                            e.PrimaryMuscleGroup,
                            e.SecondaryMuscleGroups
                        })).ToList()
                }).ToList()
            })
            .ToListAsync(ct);

        var weeklyCompliance = weeks.Select(w => new WeekCompliancePoint(
            w.WeekNumber, w.Name,
            w.Days.Count(d => d.IsCompleted),
            w.Days.Count(d => !d.IsRest)
        )).ToList();

        var weeklyVolume = weeks.Select(w => new WeekVolumePoint(
            w.WeekNumber, w.Name,
            w.Days.SelectMany(d => d.Sets)
                  .Sum(s => (s.ActualReps ?? 0) * (s.ActualWeight ?? 0m))
        )).ToList();

        var weightedEntries = weeks
            .SelectMany(w => w.Days.SelectMany(d => d.Sets.SelectMany(s =>
            {
                var setVolume = (s.ActualReps ?? 0) * (s.ActualWeight ?? 0m);
                var entries = new List<WeightedMuscleEntry>
                {
                    new(w.WeekNumber, w.Name, s.PrimaryMuscleGroup, setVolume, setVolume, 0m, 1)
                };

                entries.AddRange(s.SecondaryMuscleGroups
                    .Distinct()
                    .Where(m => m != s.PrimaryMuscleGroup)
                    .Select(m => new WeightedMuscleEntry(
                        w.WeekNumber,
                        w.Name,
                        m,
                        setVolume * 0.5m,
                        0m,
                        setVolume * 0.5m,
                        0)));

                return entries;
            })))
            .ToList();

        var totalWeightedVolume = weightedEntries.Sum(e => e.TotalVolume);
        var muscleGroups = weightedEntries
            .GroupBy(e => e.MuscleGroup)
            .Select(g =>
            {
                var total = g.Sum(e => e.TotalVolume);
                return new MuscleGroupPoint(
                    g.Key.ToString(),
                    g.Sum(e => e.CompletedSets),
                    total,
                    g.Sum(e => e.PrimaryVolume),
                    g.Sum(e => e.SecondaryVolume),
                    totalWeightedVolume == 0m ? 0m : Math.Round(total / totalWeightedVolume * 100, 1));
            })
            .OrderByDescending(m => m.TotalVolume)
            .ThenByDescending(m => m.CompletedSets)
            .ToList();

        var muscleGroupHeatmap = weightedEntries
            .GroupBy(e => e.MuscleGroup)
            .Select(g => new MuscleGroupHeatmapPoint(
                g.Key.ToString(),
                g.Sum(e => e.TotalVolume),
                weeks.Select(w => new MuscleGroupHeatmapWeekPoint(
                    w.WeekNumber,
                    w.Name,
                    g.Where(e => e.WeekNumber == w.WeekNumber).Sum(e => e.TotalVolume)
                )).ToList()))
            .OrderByDescending(m => m.TotalVolume)
            .ToList();

        var muscleGroupBalance = BuildMuscleGroupBalance(weightedEntries);

        var totalCompleted = weeks.SelectMany(w => w.Days).Count(d => d.IsCompleted);
        var nonRestDays    = weeks.SelectMany(w => w.Days).Count(d => !d.IsRest);
        var missedDays     = weeks.SelectMany(w => w.Days).Count(d => d.IsMissed);
        var warningDays    = weeks.SelectMany(w => w.Days).Count(d => d.HasWarning);
        var completedSets  = weeks.SelectMany(w => w.Days).SelectMany(d => d.Sets).ToList();
        var rpeValues      = completedSets.Where(s => s.Rpe is not null).Select(s => s.Rpe!.Value).ToList();
        var avgRpe         = rpeValues.Count == 0 ? (decimal?)null : Math.Round(rpeValues.Average(), 1);
        var highRpeSets    = rpeValues.Count(r => r >= 9m);
        var totalDurationSeconds = weeks.SelectMany(w => w.Days).SelectMany(d => d.CompletedExerciseDurations).Sum();
        var totalVolume    = weeklyVolume.Sum(w => w.TotalVolume);
        var consistency    = nonRestDays == 0 ? 0m : Math.Round((decimal)totalCompleted / nonRestDays * 100, 1);
        var avgPerWeek     = weeks.Count == 0 ? 0m : Math.Round((decimal)totalCompleted / weeks.Count, 1);
        var insightResult  = TrainingInsightAnalyzer.Analyze(new TrainingInsightInput(
            consistency,
            nonRestDays,
            missedDays,
            warningDays,
            avgRpe,
            highRpeSets,
            weeklyVolume,
            muscleGroups));

        return new PlanAnalyticsResponse(totalCompleted, totalVolume, consistency, avgPerWeek,
            completedSets.Count, avgRpe, highRpeSets, warningDays, totalDurationSeconds,
            insightResult.TrainingScore, insightResult.Insights,
            weeklyCompliance, weeklyVolume, muscleGroups, muscleGroupHeatmap, muscleGroupBalance);
    }

    public async Task<PlanExportData?> GetForExportAsync(Guid planId, Guid userId, CancellationToken ct)
    {
        // Single projection query — one round-trip, only the columns the CSV needs
        var raw = await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == planId &&
                        (p.OwnerId == userId || p.CreatedByCoachId == userId))
            .Select(p => new
            {
                p.Name,
                p.StartDate,
                Weeks = p.WeeklyWorkouts
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new
                    {
                        w.WeekNumber,
                        w.Name,
                        w.StartDate,
                        w.EndDate,
                        Days = w.DailyWorkouts
                            .OrderBy(d => d.Date)
                            .Select(d => new
                            {
                                d.Date,
                                d.DayOfWeek,
                                d.IsCompleted,
                                Exercises = d.Exercises
                                    .OrderBy(e => e.SortOrder)
                                    .Select(e => new
                                    {
                                        e.Name,
                                        e.PlannedSets,
                                        e.PlannedReps,
                                        e.PlannedWeight,
                                        CompletedSetsCount = e.Sets.Count(s => s.IsCompleted),
                                        e.IsCompleted,
                                        e.Notes
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (raw is null) return null;

        return new PlanExportData(
            raw.Name,
            raw.StartDate,
            raw.Weeks.Select(w => new WeekExportData(
                w.WeekNumber,
                w.Name,
                w.StartDate,
                w.EndDate,
                w.Days.Select(d => new DayExportData(
                    d.Date,
                    d.DayOfWeek,
                    d.IsCompleted,
                    d.Exercises.Select(e => new ExerciseExportData(
                        e.Name,
                        e.PlannedSets,
                        e.PlannedReps,
                        e.PlannedWeight,
                        e.CompletedSetsCount,
                        e.IsCompleted,
                        e.Notes
                    )).ToList()
                )).ToList()
            )).ToList()
        );
    }

    public Task<Plan?> GetForDuplicateAsync(Guid planId, Guid userId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .Include(p => p.WeeklyWorkouts)
              .ThenInclude(w => w.DailyWorkouts)
                  .ThenInclude(d => d.Exercises)
                      .ThenInclude(e => e.Sets)
          .FirstOrDefaultAsync(p => p.Id == planId &&
              (p.OwnerId == userId || p.CreatedByCoachId == userId), ct);

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
        p.WeeklyWorkouts.Count(w => w.IsCompleted),
        allDays.Count,
        allDays.Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted)),
        p.IsActive, p.CreatedAt);

    private static bool IsPlannedTrainingDay(PlannedDaySnapshot day) =>
        day.Status != DayStatus.Rest && day.Exercises.Count > 0;

    private static int LongestTrainingStreak(List<PlannedDaySnapshot> days)
    {
        var longest = 0;
        var current = 0;
        foreach (var day in days.OrderBy(d => d.Date))
        {
            if (IsPlannedTrainingDay(day))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    private static IEnumerable<PlannedMuscleEntry> BuildPlannedMuscleEntries(PlannedExerciseSnapshot exercise)
    {
        if (exercise.PlannedSets <= 0)
            yield break;

        yield return new PlannedMuscleEntry(exercise.PrimaryMuscleGroup, exercise.PlannedSets, exercise.PlannedSets, 0m);

        foreach (var secondary in exercise.SecondaryMuscleGroups.Distinct().Where(m => m != exercise.PrimaryMuscleGroup))
            yield return new PlannedMuscleEntry(secondary, exercise.PlannedSets * 0.5m, 0m, exercise.PlannedSets * 0.5m);
    }

    private static string MuscleStatus(decimal percent) =>
        percent >= 25m ? "Dominant" :
        percent < 5m ? "Low" :
        "Balanced";

    private static PlanDesignBalanceResponse BuildPlanDesignBalance(
        List<PlannedMuscleEntry> entries,
        List<PlannedMuscleGroupPoint> muscleGroups)
    {
        var front = SumPlanned(entries, MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Biceps,
            MuscleGroup.Abs, MuscleGroup.Quads, MuscleGroup.Adductors, MuscleGroup.Abductors);
        var back = SumPlanned(entries, MuscleGroup.Back, MuscleGroup.Triceps, MuscleGroup.Forearms,
            MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Traps, MuscleGroup.Neck);
        var upper = SumPlanned(entries, MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
            MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Forearms, MuscleGroup.Abs,
            MuscleGroup.Traps, MuscleGroup.Neck);
        var lower = SumPlanned(entries, MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes,
            MuscleGroup.Calves, MuscleGroup.Adductors, MuscleGroup.Abductors);
        var known = front + back;
        var total = entries.Sum(e => e.WeightedSets);
        var other = Math.Max(0m, total - known);
        var max = new[] { front, back, upper, lower, other }.Max();
        var present = muscleGroups.ToDictionary(m => m.MuscleGroup, m => m.PercentOfTotal, StringComparer.OrdinalIgnoreCase);
        var majorGroups = new[] { "Chest", "Back", "Shoulders", "Quads", "Hamstrings", "Glutes", "Abs" };

        return new PlanDesignBalanceResponse(
            front,
            back,
            upper,
            lower,
            other,
            max,
            muscleGroups.Where(m => m.PercentOfTotal >= 25m).Select(m => m.MuscleGroup).ToList(),
            majorGroups.Where(m => !present.TryGetValue(m, out var percent) || percent < 5m).ToList());
    }

    private static decimal SumPlanned(List<PlannedMuscleEntry> entries, params MuscleGroup[] muscleGroups)
    {
        var set = muscleGroups.ToHashSet();
        return entries.Where(e => set.Contains(e.MuscleGroup)).Sum(e => e.WeightedSets);
    }

    private static List<MovementPatternCoveragePoint> BuildMovementPatterns(List<PlannedExerciseSnapshot> exercises)
    {
        var patterns = new[]
        {
            "Squat/Lunge",
            "Hinge",
            "Horizontal Push",
            "Vertical Push",
            "Horizontal Pull",
            "Vertical Pull",
            "Core",
            "Carry/Cardio/Isolation"
        };

        return patterns.Select(pattern =>
        {
            var matching = exercises.Where(e => MatchesPattern(e, pattern)).ToList();
            return new MovementPatternCoveragePoint(
                pattern,
                matching.Count > 0,
                matching.Count,
                matching.Sum(e => e.PlannedSets));
        }).ToList();
    }

    private static bool MatchesPattern(PlannedExerciseSnapshot exercise, string pattern)
    {
        var name = Normalize(exercise.Name);
        return pattern switch
        {
            "Squat/Lunge" => name.Contains("squat") || name.Contains("lunge") ||
                name.Contains("legpress") || name.Contains("stepup") || exercise.PrimaryMuscleGroup == MuscleGroup.Quads,
            "Hinge" => name.Contains("deadlift") || name.Contains("romanian") ||
                name.Contains("hipthrust") || name.Contains("goodmorning") || name.Contains("kettlebellswing") ||
                exercise.PrimaryMuscleGroup is MuscleGroup.Hamstrings or MuscleGroup.Glutes,
            "Horizontal Push" => name.Contains("bench") || name.Contains("pushup") || name.Contains("dip") ||
                name.Contains("fly") || exercise.PrimaryMuscleGroup == MuscleGroup.Chest,
            "Vertical Push" => name.Contains("overhead") || name.Contains("shoulderpress") ||
                name.Contains("arnold") || name.Contains("militarypress"),
            "Horizontal Pull" => name.Contains("row") || name.Contains("facepull") ||
                name.Contains("tbar") || name.Contains("pendlay"),
            "Vertical Pull" => name.Contains("pullup") || name.Contains("chinup") ||
                name.Contains("pulldown"),
            "Core" => exercise.PrimaryMuscleGroup == MuscleGroup.Abs || name.Contains("plank") ||
                name.Contains("crunch") || name.Contains("legraise") || name.Contains("twist") ||
                name.Contains("pallof") || name.Contains("abwheel"),
            "Carry/Cardio/Isolation" => exercise.PrimaryMuscleGroup is MuscleGroup.Cardio or MuscleGroup.Biceps or
                MuscleGroup.Triceps or MuscleGroup.Calves or MuscleGroup.Forearms || name.Contains("carry") ||
                name.Contains("walk") || name.Contains("curl") || name.Contains("raise") || name.Contains("pushdown"),
            _ => false
        };
    }

    private static List<RecoveryRiskPoint> BuildRecoveryRisks(
        List<PlannedDaySnapshot> days,
        List<PlannedDaySnapshot> trainingDays)
    {
        var risks = new List<RecoveryRiskPoint>();
        var orderedTrainingDays = trainingDays.OrderBy(d => d.Date).ToList();
        for (var i = 1; i < orderedTrainingDays.Count; i++)
        {
            var previous = orderedTrainingDays[i - 1];
            var current = orderedTrainingDays[i];
            if (current.Date.DayNumber - previous.Date.DayNumber != 1)
                continue;

            var overlap = previous.Exercises.Select(e => e.PrimaryMuscleGroup)
                .Intersect(current.Exercises.Select(e => e.PrimaryMuscleGroup))
                .ToList();
            if (overlap.Count > 0)
            {
                risks.Add(new RecoveryRiskPoint(
                    "RepeatedMuscle",
                    "Medium",
                    $"Back-to-back training days repeat {string.Join(", ", overlap)}.",
                    $"{previous.Date:dd/MM} -> {current.Date:dd/MM}"));
            }

            if (IsLowerOrSpinalDay(previous) && IsLowerOrSpinalDay(current))
            {
                risks.Add(new RecoveryRiskPoint(
                    "LowerBodySpacing",
                    "High",
                    "Lower-body or spinal-loading days are scheduled on consecutive days.",
                    $"{previous.Date:dd/MM} -> {current.Date:dd/MM}"));
            }
        }

        var longestStreak = LongestTrainingStreak(days);
        if (longestStreak >= 4)
        {
            risks.Add(new RecoveryRiskPoint(
                "RestDistribution",
                "High",
                "The plan has a long training streak without a rest day.",
                $"{longestStreak} days"));
        }
        else if (longestStreak >= 3)
        {
            risks.Add(new RecoveryRiskPoint(
                "RestDistribution",
                "Medium",
                "The plan clusters several training days in a row.",
                $"{longestStreak} days"));
        }

        var averageSets = trainingDays.Count == 0
            ? 0m
            : trainingDays.Average(d => (decimal)d.Exercises.Sum(e => e.PlannedSets));
        foreach (var day in trainingDays)
        {
            var daySets = day.Exercises.Sum(e => e.PlannedSets);
            if (averageSets > 0 && daySets >= averageSets * 1.5m && daySets >= averageSets + 4m)
            {
                risks.Add(new RecoveryRiskPoint(
                    "DenseDay",
                    "Medium",
                    "One training day is much denser than the plan average.",
                    $"{day.Date:dd/MM}: {daySets} sets"));
            }
        }

        return risks
            .GroupBy(r => new { r.Type, r.Metric })
            .Select(g => g.First())
            .Take(8)
            .ToList();
    }

    private static bool IsLowerOrSpinalDay(PlannedDaySnapshot day) =>
        day.Exercises.Any(e =>
            e.PrimaryMuscleGroup is MuscleGroup.Quads or MuscleGroup.Hamstrings or MuscleGroup.Glutes or
                MuscleGroup.Adductors or MuscleGroup.Abductors or MuscleGroup.Back or MuscleGroup.Traps ||
            Normalize(e.Name).Contains("deadlift") ||
            Normalize(e.Name).Contains("squat") ||
            Normalize(e.Name).Contains("row") ||
            Normalize(e.Name).Contains("goodmorning"));

    private static string Normalize(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static MuscleGroupBalancePoint BuildMuscleGroupBalance(List<WeightedMuscleEntry> entries)
    {
        var front = Sum(entries, MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Biceps,
            MuscleGroup.Abs, MuscleGroup.Quads, MuscleGroup.Adductors, MuscleGroup.Abductors);
        var back = Sum(entries, MuscleGroup.Back, MuscleGroup.Triceps, MuscleGroup.Forearms,
            MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Traps, MuscleGroup.Neck);
        var upper = Sum(entries, MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
            MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Forearms, MuscleGroup.Abs,
            MuscleGroup.Traps, MuscleGroup.Neck);
        var lower = Sum(entries, MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes,
            MuscleGroup.Calves, MuscleGroup.Adductors, MuscleGroup.Abductors);
        var known = front + back;
        var total = entries.Sum(e => e.TotalVolume);
        var other = Math.Max(0m, total - known);
        var max = new[] { front, back, upper, lower, other }.Max();

        return new MuscleGroupBalancePoint(front, back, upper, lower, other, max);
    }

    private static decimal Sum(List<WeightedMuscleEntry> entries, params MuscleGroup[] muscleGroups)
    {
        var set = muscleGroups.ToHashSet();
        return entries.Where(e => set.Contains(e.MuscleGroup)).Sum(e => e.TotalVolume);
    }

    private sealed record WeightedMuscleEntry(
        int WeekNumber,
        string WeekName,
        MuscleGroup MuscleGroup,
        decimal TotalVolume,
        decimal PrimaryVolume,
        decimal SecondaryVolume,
        int CompletedSets
    );

    private sealed record PlannedDaySnapshot(
        DateOnly Date,
        DayStatus Status,
        List<PlannedExerciseSnapshot> Exercises)
    {
        public int WeekNumber { get; init; }
        public string WeekName { get; init; } = string.Empty;
    }

    private sealed record PlannedExerciseSnapshot(
        string Name,
        MuscleGroup PrimaryMuscleGroup,
        List<MuscleGroup> SecondaryMuscleGroups,
        int PlannedSets,
        int PlannedReps,
        decimal? PlannedWeight
    );

    private sealed record PlannedMuscleEntry(
        MuscleGroup MuscleGroup,
        decimal WeightedSets,
        decimal PrimarySets,
        decimal SecondarySets
    );
}
