using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IPaymentOrderRepository
{
    Task<PaymentOrder?> FindByTransferCodeAsync(string transferCode, CancellationToken ct = default);
    Task<PaymentOrder?> FindBySePayTransactionIdAsync(string sePayTransactionId, CancellationToken ct = default);
    Task<PaymentOrder?> FindPendingByUserAndTierAsync(Guid userId, PlanTier tier, CancellationToken ct = default);
    Task<PaymentOrder?> FindLatestPendingByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(PaymentOrder order, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
