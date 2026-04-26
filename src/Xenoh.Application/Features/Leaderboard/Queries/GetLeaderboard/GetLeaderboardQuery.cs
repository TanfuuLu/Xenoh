using Mediator;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery : IRequest<List<LeaderboardEntryResponse>>
{
    /// <summary>dots | squat | bench | deadlift</summary>
    public string Type { get; init; } = "dots";

    /// <summary>male | female — optional filter</summary>
    public string? Gender { get; init; }
}

public sealed record LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string FullName,
    decimal Score,
    decimal? BodyweightKg
);
