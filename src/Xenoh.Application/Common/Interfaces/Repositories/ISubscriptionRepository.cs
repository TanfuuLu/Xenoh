using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<UserSubscription?> FindByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserSubscription?> GetByUserIdAsNoTrackingAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserSubscription subscription, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
