using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
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
              p.WeeklyWorkouts.Sum(w => w.DailyWorkouts.Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted))),
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
                    .ThenInclude(d => d.Exercises)
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
          .Where(p =>
              p.PlanType == PlanType.Coach &&
              p.CreatedByCoachId == coachId &&
              p.OwnerId != coachId)
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
            insightResult.TrainingScore, insightResult.Insights,
            weeklyCompliance, weeklyVolume, muscleGroups, muscleGroupHeatmap, muscleGroupBalance);
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
        allDays.Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted)),
        p.IsActive, p.CreatedAt);

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
}
