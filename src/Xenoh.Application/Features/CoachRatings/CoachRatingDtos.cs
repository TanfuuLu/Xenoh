namespace Xenoh.Application.Features.CoachRatings;

public sealed record CoachRatingSummary(
    decimal? AverageRating,
    int RatingCount,
    CoachRatingResponse? MyRating
);

public sealed record CoachRatingResponse(
    Guid Id,
    Guid CoachId,
    Guid ClientId,
    string ClientName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
