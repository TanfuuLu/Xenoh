using Mediator;
using Xenoh.Application.Features.Coaches;
using Xenoh.Application.Features.CoachRatings;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoaches;

public sealed record GetCoachesQuery(string? Name = null) : IRequest<List<CoachResponse>>;

public sealed record CoachResponse(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    decimal? AverageRating,
    int RatingCount,
    CoachRatingResponse? MyRating,
    string? Headline,
    string[] Specialties,
    int? ExperienceYears,
    decimal? StartingPackagePrice,
    int PackageCount
);
