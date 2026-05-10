using Mediator;
using Xenoh.Application.Features.Coaches;
using Xenoh.Application.Features.CoachRatings;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

public sealed record GetCoachProfileQuery(Guid CoachId) : IRequest<CoachProfileResponse>;

public sealed record CoachProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? Bio,
    int TotalClients,
    decimal? AverageRating,
    int RatingCount,
    bool CanRate,
    CoachRatingResponse? MyRating,
    List<CoachRatingResponse> Ratings,
    CoachMarketplaceProfileDto? MarketplaceProfile
);
