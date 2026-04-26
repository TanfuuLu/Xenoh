using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindActiveAsync(string token, CancellationToken ct = default);
    Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
