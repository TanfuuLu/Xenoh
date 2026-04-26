namespace Xenoh.Application.Features.Users.Queries.GetMyProfile;

public sealed record Big3PrsResponse(
    decimal? Squat,
    decimal? Bench,
    decimal? Deadlift
);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    decimal? Height,
    string? Gender,
    DateOnly? DateOfBirth,
    int CurrentStreak,
    decimal? LatestBodyweight,
    decimal? Bmi,
    string? BmiCategory,
    decimal? DotsScore,
    Big3PrsResponse Big3Prs
);
