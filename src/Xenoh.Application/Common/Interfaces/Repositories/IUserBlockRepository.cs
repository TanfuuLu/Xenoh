using Xenoh.Application.Features.Blocks.Queries.GetMyBlocks;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IUserBlockRepository
{
    Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);
    Task<bool> IsEitherBlockedAsync(Guid userA, Guid userB, CancellationToken ct = default);
    Task<UserBlock?> FindAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);
    Task<List<BlockedUserResponse>> ListByBlockerAsync(Guid blockerId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetBlockedOrBlockerIdsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserBlock block, CancellationToken ct = default);
    void Remove(UserBlock block);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
