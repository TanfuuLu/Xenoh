using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;

public sealed class GetLeaderboardHandler(
    ILeaderboardRepository leaderboardRepo,
    IApplicationCache? cache = null)
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryResponse>>
{
    private const int MaxEntries = 50;

    public ValueTask<List<LeaderboardEntryResponse>> Handle(
        GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var type = request.Type.ToLowerInvariant();
        var gender = ParseGender(request.Gender);

        return new ValueTask<List<LeaderboardEntryResponse>>(cache is null
            ? LoadAsync(type, gender, cancellationToken)
            : cache.GetOrCreateAsync(
                CacheTags.Leaderboards,
                $"lift:{type}:gender:{request.Gender?.ToLowerInvariant() ?? "all"}",
                TimeSpan.FromSeconds(30),
                ct => LoadAsync(type, gender, ct),
                cancellationToken));
    }

    private Task<List<LeaderboardEntryResponse>> LoadAsync(
        string type,
        Gender? gender,
        CancellationToken cancellationToken)
    {
        if (type is "squat" or "bench" or "deadlift")
        {
            var lift = type switch
            {
                "squat" => CompetitionLiftType.Squat,
                "bench" => CompetitionLiftType.Bench,
                _ => CompetitionLiftType.Deadlift
            };
            return leaderboardRepo.GetSingleLiftAsync(lift, gender, MaxEntries, cancellationToken);
        }

        return leaderboardRepo.GetDotsAsync(gender, MaxEntries, cancellationToken);
    }

    private static Gender? ParseGender(string? gender) => gender?.ToLowerInvariant() switch
    {
        "male" => Gender.Male,
        "female" => Gender.Female,
        _ => null
    };
}
