using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Blocks.Queries.GetMyBlocks;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class UserBlockRepository(ApplicationDbContext db) : IUserBlockRepository
{
    public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken ct) =>
        db.UserBlocks
          .AsNoTracking()
          .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, ct);

    public Task<bool> IsEitherBlockedAsync(Guid userA, Guid userB, CancellationToken ct) =>
        db.UserBlocks
          .AsNoTracking()
          .AnyAsync(b =>
              (b.BlockerId == userA && b.BlockedId == userB) ||
              (b.BlockerId == userB && b.BlockedId == userA), ct);

    public Task<UserBlock?> FindAsync(Guid blockerId, Guid blockedId, CancellationToken ct) =>
        db.UserBlocks
          .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, ct);

    public Task<List<BlockedUserResponse>> ListByBlockerAsync(Guid blockerId, CancellationToken ct) =>
        db.UserBlocks
          .AsNoTracking()
          .Include(b => b.Blocked)
          .Where(b => b.BlockerId == blockerId)
          .OrderByDescending(b => b.CreatedAt)
          .Select(b => new BlockedUserResponse(
              b.Id,
              b.BlockedId,
              b.Blocked.FirstName + " " + b.Blocked.LastName,
              b.Blocked.AvatarUrl,
              b.Reason,
              b.CreatedAt))
          .ToListAsync(ct);

    public async Task<HashSet<Guid>> GetBlockedOrBlockerIdsAsync(Guid userId, CancellationToken ct)
    {
        var ids = await db.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync(ct);

        return [.. ids];
    }

    public async Task AddAsync(UserBlock block, CancellationToken ct)
    {
        await db.UserBlocks.AddAsync(block, ct);
    }

    public void Remove(UserBlock block) => db.UserBlocks.Remove(block);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
