using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class PaymentOrderRepository(ApplicationDbContext db) : IPaymentOrderRepository
{
    public Task<PaymentOrder?> FindByTransferCodeAsync(string transferCode, CancellationToken ct) =>
        db.PaymentOrders.FirstOrDefaultAsync(o => o.TransferCode == transferCode, ct);

    public Task<PaymentOrder?> FindBySePayTransactionIdAsync(string sePayTransactionId, CancellationToken ct) =>
        db.PaymentOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.SePayTransactionId == sePayTransactionId, ct);

    public Task<PaymentOrder?> FindPendingByUserAndTierAsync(Guid userId, PlanTier tier, CancellationToken ct) =>
        db.PaymentOrders.FirstOrDefaultAsync(
            o => o.UserId == userId && o.RequestedTier == tier && o.Status == PaymentStatus.Pending, ct);

    public Task<PaymentOrder?> FindLatestPendingByUserAsync(Guid userId, CancellationToken ct) =>
        db.PaymentOrders
            .Where(o => o.UserId == userId && o.Status == PaymentStatus.Pending)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(PaymentOrder order, CancellationToken ct) =>
        await db.PaymentOrders.AddAsync(order, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
