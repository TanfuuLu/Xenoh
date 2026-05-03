using Xenoh.Application.Features.CoachRatings;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface ICoachRatingRepository
{
    Task<CoachRating?> FindAsync(Guid coachId, Guid clientId, CancellationToken ct = default);
    Task<bool> CanClientRateCoachAsync(Guid coachId, Guid clientId, CancellationToken ct = default);
    Task<CoachRatingSummary> GetSummaryAsync(Guid coachId, Guid? clientId = null, CancellationToken ct = default);
    Task<List<CoachRatingResponse>> GetByCoachAsync(Guid coachId, CancellationToken ct = default);
    Task AddAsync(CoachRating rating, CancellationToken ct = default);
    void Remove(CoachRating rating);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
