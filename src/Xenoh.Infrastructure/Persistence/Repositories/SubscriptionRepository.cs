using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionRepository(ApplicationDbContext db) : ISubscriptionRepository
{
    public Task<UserSubscription?> FindByUserIdAsync(Guid userId, CancellationToken ct) =>
        db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public Task<UserSubscription?> GetByUserIdAsNoTrackingAsync(Guid userId, CancellationToken ct) =>
        db.UserSubscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task AddAsync(UserSubscription subscription, CancellationToken ct) =>
        await db.UserSubscriptions.AddAsync(subscription, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
