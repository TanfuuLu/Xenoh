using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Application.Features.CoachClient.Queries.GetMyClients;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class CoachClientRepository(ApplicationDbContext db) : ICoachClientRepository
{
    public Task<CoachClientRelationship?> FindByClientAsync(Guid clientId, CancellationToken ct) =>
        db.CoachClientRelationships
          .FirstOrDefaultAsync(r => r.ClientId == clientId && r.Status != RelationshipStatus.Ended, ct);

    public Task<CoachClientRelationship?> FindByIdForCoachAsync(Guid id, Guid coachId, CancellationToken ct) =>
        db.CoachClientRelationships
          .Include(r => r.Client)
          .Include(r => r.Coach)
          .FirstOrDefaultAsync(r => r.Id == id && r.CoachId == coachId, ct);

    public Task<CoachClientRelationship?> FindByIdForParticipantAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.CoachClientRelationships
          .FirstOrDefaultAsync(r => r.Id == id && (r.ClientId == userId || r.CoachId == userId), ct);

    public Task<CoachClientRelationship?> FindActiveByCoachAndClientAsync(Guid coachId, Guid clientId, CancellationToken ct) =>
        db.CoachClientRelationships
          .FirstOrDefaultAsync(r =>
              r.CoachId == coachId &&
              r.ClientId == clientId &&
              r.Status == RelationshipStatus.Active, ct);

    public Task<bool> HasActiveRelationshipAsync(Guid userId1, Guid userId2, CancellationToken ct) =>
        db.CoachClientRelationships
          .AsNoTracking()
          .AnyAsync(r =>
              r.Status == RelationshipStatus.Active &&
              ((r.CoachId == userId1 && r.ClientId == userId2) ||
               (r.ClientId == userId1 && r.CoachId == userId2)), ct);

    public Task<List<CoachRelationshipResponse>> GetPendingByCoachAsync(Guid coachId, CancellationToken ct) =>
        db.CoachClientRelationships
          .AsNoTracking()
          .Include(r => r.Client)
          .Include(r => r.Coach)
          .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Pending)
          .Select(r => new CoachRelationshipResponse(
              r.Id,
              r.ClientId,
              r.Client.FirstName + " " + r.Client.LastName,
              r.Client.AvatarUrl,
              r.CoachId,
              r.Coach.FirstName + " " + r.Coach.LastName,
              r.Status.ToString(),
              r.CreatedAt,
              null))
          .ToListAsync(ct);

    public async Task<CoachRelationshipResponse?> GetByClientWithDetailsAsync(Guid clientId, CancellationToken ct)
    {
        var r = await db.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.Coach)
            .FirstOrDefaultAsync(r => r.ClientId == clientId && r.Status != RelationshipStatus.Ended, ct);

        return r is null ? null : new CoachRelationshipResponse(
            r.Id,
            r.ClientId,
            $"{r.Client.FirstName} {r.Client.LastName}",
            r.Client.AvatarUrl,
            r.CoachId,
            $"{r.Coach.FirstName} {r.Coach.LastName}",
            r.Status.ToString(),
            r.CreatedAt,
            r.TerminationRequestedBy);
    }

    public Task<List<ClientResponse>> GetAllByCoachAsync(Guid coachId, CancellationToken ct) =>
        db.CoachClientRelationships
          .AsNoTracking()
          .Include(r => r.Client)
          .Where(r => r.CoachId == coachId)
          .Where(r => r.Status != RelationshipStatus.Ended)
          .OrderByDescending(r => r.CreatedAt)
          .Select(r => new ClientResponse(
              r.Id,
              r.ClientId,
              $"{r.Client.FirstName} {r.Client.LastName}",
              r.Client.Email!,
              r.Status.ToString(),
              r.CreatedAt,
              db.WorkoutHistories
                .Where(w => w.UserId == r.ClientId)
                .OrderByDescending(w => w.Date)
                .Select(w => (DateOnly?)w.Date)
                .FirstOrDefault(),
              r.TerminationRequestedBy))
          .ToListAsync(ct);

    public Task<int> CountActiveByCoachAsync(Guid coachId, CancellationToken ct) =>
        db.CoachClientRelationships
          .AsNoTracking()
          .CountAsync(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active, ct);

    public async Task AddAsync(CoachClientRelationship relationship, CancellationToken ct)
    {
        await db.CoachClientRelationships.AddAsync(relationship, ct);
    }

    public void Remove(CoachClientRelationship relationship) =>
        db.CoachClientRelationships.Remove(relationship);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
