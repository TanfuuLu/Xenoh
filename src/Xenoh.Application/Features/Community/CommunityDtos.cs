namespace Xenoh.Application.Features.Community;

public sealed record CommunityUserSummaryResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    string FriendStatus,
    Guid? FriendshipId,
    string? RequestDirection);

public sealed record CommunityBig3PrsResponse(
    decimal? Squat,
    decimal? Bench,
    decimal? Deadlift);

public sealed record CommunityUserProfileResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    string? DevelopmentDirection,
    string? TrainingDiscipline,
    string FriendStatus,
    Guid? FriendshipId,
    string? RequestDirection,
    bool CanViewStats,
    decimal? Height,
    decimal? LatestBodyweight,
    decimal? Bmi,
    string? BmiCategory,
    int? CurrentStreak,
    decimal? DotsScore,
    long TotalTrainingDurationSeconds,
    decimal TotalTrainingVolume,
    CommunityBig3PrsResponse Big3Prs,
    decimal? Big3Total,
    int Level);
