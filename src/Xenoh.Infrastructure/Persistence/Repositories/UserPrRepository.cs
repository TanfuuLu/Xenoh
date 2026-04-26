using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class UserPrRepository(ApplicationDbContext db) : IUserPrRepository
{
    public async Task<Dictionary<CompetitionLiftType, decimal?>> GetBig3Async(Guid userId, CancellationToken ct) =>
        await (
            from pr in db.UserExercisePRs.AsNoTracking()
            join t in db.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            where pr.UserId == userId && t.IsCompetitionLift
            select new { t.CompetitionLiftType, Weight = (decimal?)pr.Weight }
        ).ToDictionaryAsync(x => x.CompetitionLiftType!.Value, x => x.Weight, ct);

    public async Task<Dictionary<Guid, decimal?>> GetByTemplateIdsAsync(
        Guid userId, IEnumerable<Guid> templateIds, CancellationToken ct) =>
        await db.UserExercisePRs
          .AsNoTracking()
          .Where(p => p.UserId == userId && templateIds.Contains(p.ExerciseTemplateId))
          .ToDictionaryAsync(p => p.ExerciseTemplateId, p => (decimal?)p.Weight, ct);

    public async Task<List<(Guid UserId, CompetitionLiftType LiftType, decimal Weight)>> GetCompetitionLiftsForUsersAsync(
        IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        var rows = await (
            from pr in db.UserExercisePRs.AsNoTracking()
            join t in db.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            where ids.Contains(pr.UserId) && t.IsCompetitionLift && t.CompetitionLiftType != null
            select new { pr.UserId, LiftType = t.CompetitionLiftType!.Value, pr.Weight }
        ).ToListAsync(ct);

        return rows.Select(r => (r.UserId, r.LiftType, r.Weight)).ToList();
    }

    public Task<UserExercisePR?> FindAsync(Guid userId, Guid exerciseTemplateId, CancellationToken ct) =>
        db.UserExercisePRs
          .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseTemplateId == exerciseTemplateId, ct);

    public async Task AddAsync(UserExercisePR pr, CancellationToken ct) =>
        await db.UserExercisePRs.AddAsync(pr, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
