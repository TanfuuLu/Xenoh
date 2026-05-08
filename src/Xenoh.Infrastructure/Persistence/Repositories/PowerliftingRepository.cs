using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class PowerliftingRepository(ApplicationDbContext db) : IPowerliftingRepository
{
    public Task<bool> PlanHasCompetitionLiftsAsync(Guid planId, Guid ownerId, CancellationToken ct) =>
        db.Plans
          .AsNoTracking()
          .Where(p => p.Id == planId && (p.OwnerId == ownerId || p.CreatedByCoachId == ownerId))
          .SelectMany(p => p.WeeklyWorkouts)
          .SelectMany(w => w.DailyWorkouts)
          .SelectMany(d => d.Exercises)
          .AnyAsync(e => e.ExerciseTemplate.IsCompetitionLift
                      && e.ExerciseTemplate.CompetitionLiftType != null, ct);

    public async Task<IReadOnlyList<CompletedLiftSet>> GetCompletedSetsForPlanAsync(
        Guid planId, Guid ownerId, CompetitionLiftType lift, CancellationToken ct)
    {
        var rows = await (
            from p in db.Plans.AsNoTracking()
            where p.Id == planId && (p.OwnerId == ownerId || p.CreatedByCoachId == ownerId)
            from w in p.WeeklyWorkouts
            from d in w.DailyWorkouts
            from ex in d.Exercises
            where ex.ExerciseTemplate.IsCompetitionLift
               && ex.ExerciseTemplate.CompetitionLiftType == lift
            from s in ex.Sets
            where s.IsCompleted
               && s.ActualWeight != null
               && s.ActualReps != null
               && s.ActualWeight > 0m
               && s.ActualReps > 0
            select new
            {
                Date = d.Date,
                Weight = s.ActualWeight!.Value,
                Reps = s.ActualReps!.Value,
                Rpe = s.Rpe
            }
        ).ToListAsync(ct);

        return rows
            .Select(r => new CompletedLiftSet(r.Date, r.Weight, r.Reps, r.Rpe))
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<CompletedLiftSet>> GetCompletedSetsForUserAsync(
        Guid userId, CompetitionLiftType lift, CancellationToken ct)
    {
        var rows = await (
            from p in db.Plans.AsNoTracking()
            where p.OwnerId == userId
            from w in p.WeeklyWorkouts
            from d in w.DailyWorkouts
            from ex in d.Exercises
            where ex.ExerciseTemplate.IsCompetitionLift
               && ex.ExerciseTemplate.CompetitionLiftType == lift
            from s in ex.Sets
            where s.IsCompleted
               && s.ActualWeight != null
               && s.ActualReps != null
               && s.ActualWeight > 0m
               && s.ActualReps > 0
            select new
            {
                Date = d.Date,
                Weight = s.ActualWeight!.Value,
                Reps = s.ActualReps!.Value,
                Rpe = s.Rpe
            }
        ).ToListAsync(ct);

        return rows
            .Select(r => new CompletedLiftSet(r.Date, r.Weight, r.Reps, r.Rpe))
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<BodyweightPoint>> GetBodyweightHistoryAsync(
        Guid userId, CancellationToken ct)
    {
        var rows = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Date)
            .Select(b => new { b.Date, b.Weight })
            .ToListAsync(ct);

        return rows.Select(r => new BodyweightPoint(r.Date, r.Weight)).ToList();
    }
}
