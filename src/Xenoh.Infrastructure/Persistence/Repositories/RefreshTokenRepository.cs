using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindActiveAsync(string token, CancellationToken ct) =>
        db.RefreshTokens
          .Include(r => r.User)
          .FirstOrDefaultAsync(r => r.Token == token, ct);

    public Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct) =>
        db.RefreshTokens
          .Where(r => r.UserId == userId && !r.IsRevoked)
          .ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        await db.RefreshTokens.AddAsync(token, ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
