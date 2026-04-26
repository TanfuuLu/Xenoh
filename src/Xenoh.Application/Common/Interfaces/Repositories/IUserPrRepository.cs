using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IUserPrRepository
{
    Task<Dictionary<CompetitionLiftType, decimal?>> GetBig3Async(Guid userId, CancellationToken ct = default);
    Task<Dictionary<Guid, decimal?>> GetByTemplateIdsAsync(Guid userId, IEnumerable<Guid> templateIds, CancellationToken ct = default);
    Task<List<(Guid UserId, CompetitionLiftType LiftType, decimal Weight)>> GetCompetitionLiftsForUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<UserExercisePR?> FindAsync(Guid userId, Guid exerciseTemplateId, CancellationToken ct = default);
    Task AddAsync(UserExercisePR pr, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
