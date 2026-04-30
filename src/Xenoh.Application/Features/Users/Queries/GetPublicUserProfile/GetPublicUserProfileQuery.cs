using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetPublicUserProfile;

public sealed record GetPublicUserProfileQuery(Guid UserId) : IRequest<PublicUserProfileResponse>;

public sealed record PublicUserProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    decimal? Height,
    decimal? LatestBodyweight,
    decimal? Bmi,
    string? BmiCategory,
    int CurrentStreak,
    decimal? DotsScore
);
