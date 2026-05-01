using Xenoh.Application.Features.Users.Commands.LogBodyweight;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IBodyweightRepository
{
    Task<List<BodyweightLogResponse>> GetHistoryAsync(Guid userId, DateOnly from, CancellationToken ct = default);
    Task<decimal?> GetLatestWeightAsync(Guid userId, CancellationToken ct = default);
    Task<decimal?> GetLatestWeightOnOrBeforeAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<Dictionary<Guid, decimal?>> GetLatestWeightsForUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<BodyweightLog?> FindTodayAsync(Guid userId, DateOnly today, CancellationToken ct = default);
    Task<BodyweightLog?> FindByIdAndOwnerAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(BodyweightLog log, CancellationToken ct = default);
    void Remove(BodyweightLog log);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
