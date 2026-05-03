using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachRatings;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class CoachRatingRepository(ApplicationDbContext db) : ICoachRatingRepository
{
    public Task<CoachRating?> FindAsync(Guid coachId, Guid clientId, CancellationToken ct = default) =>
        db.CoachRatings.FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId, ct);

    public Task<bool> CanClientRateCoachAsync(Guid coachId, Guid clientId, CancellationToken ct = default) =>
        db.CoachClientRelationships
            .AsNoTracking()
            .AnyAsync(r => r.CoachId == coachId &&
                           r.ClientId == clientId &&
                           (r.Status == RelationshipStatus.Active || r.Status == RelationshipStatus.Ended), ct);

    public async Task<CoachRatingSummary> GetSummaryAsync(Guid coachId, Guid? clientId = null, CancellationToken ct = default)
    {
        var ratings = db.CoachRatings.AsNoTracking().Where(r => r.CoachId == coachId);
        var aggregate = await ratings
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Average = g.Average(r => (decimal)r.Rating)
            })
            .FirstOrDefaultAsync(ct);

        CoachRatingResponse? myRating = null;
        if (clientId.HasValue)
        {
            myRating = await ratings
                .Where(r => r.ClientId == clientId.Value)
                .Include(r => r.Client)
                .Select(r => ToResponse(r))
                .FirstOrDefaultAsync(ct);
        }

        return new CoachRatingSummary(
            aggregate is null ? null : Math.Round(aggregate.Average, 1),
            aggregate?.Count ?? 0,
            myRating);
    }

    public Task<List<CoachRatingResponse>> GetByCoachAsync(Guid coachId, CancellationToken ct = default) =>
        db.CoachRatings
            .AsNoTracking()
            .Include(r => r.Client)
            .Where(r => r.CoachId == coachId)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync(ct);

    public async Task AddAsync(CoachRating rating, CancellationToken ct = default) =>
        await db.CoachRatings.AddAsync(rating, ct);

    public void Remove(CoachRating rating) => db.CoachRatings.Remove(rating);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static CoachRatingResponse ToResponse(CoachRating r) => new(
        r.Id,
        r.CoachId,
        r.ClientId,
        $"{r.Client.FirstName} {r.Client.LastName}".Trim(),
        r.Rating,
        r.Comment,
        r.CreatedAt,
        r.UpdatedAt);
}
