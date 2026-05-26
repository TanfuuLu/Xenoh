using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.CoachClient.Queries.GetMyClients;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface ICoachClientRepository
{
    Task<CoachClientRelationship?> FindByClientAsync(Guid clientId, CancellationToken ct = default);
    Task<CoachClientRelationship?> FindByIdForCoachAsync(Guid id, Guid coachId, CancellationToken ct = default);
    Task<CoachClientRelationship?> FindByIdForParticipantAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<CoachClientRelationship?> FindActiveByCoachAndClientAsync(Guid coachId, Guid clientId, CancellationToken ct = default);
    Task<bool> HasActiveRelationshipAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<List<CoachRelationshipResponse>> GetPendingByCoachAsync(Guid coachId, CancellationToken ct = default);
    Task<CoachRelationshipResponse?> GetByClientWithDetailsAsync(Guid clientId, CancellationToken ct = default);
    Task<List<ClientResponse>> GetAllByCoachAsync(Guid coachId, CancellationToken ct = default);
    Task<int> CountActiveByCoachAsync(Guid coachId, CancellationToken ct = default);
    Task<int> CountOverlappingActiveByCoachAsync(Guid coachId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task AddAsync(CoachClientRelationship relationship, CancellationToken ct = default);
    void Remove(CoachClientRelationship relationship);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
