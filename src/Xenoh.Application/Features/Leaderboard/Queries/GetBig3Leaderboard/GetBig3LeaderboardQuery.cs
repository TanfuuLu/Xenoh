using Mediator;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;

public sealed record GetBig3LeaderboardQuery : IRequest<List<Big3LeaderboardEntryResponse>>
{
    /// <summary>male | female — optional filter</summary>
    public string? Gender { get; init; }
}

public sealed record Big3LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string FullName,
    decimal? SquatPr,
    decimal? BenchPr,
    decimal? DeadliftPr,
    decimal Total,
    decimal? DotsScore
);
