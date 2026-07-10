using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;

public sealed class GetBig3LeaderboardHandler(
    ILeaderboardRepository leaderboardRepo,
    IApplicationCache? cache = null)
    : IRequestHandler<GetBig3LeaderboardQuery, List<Big3LeaderboardEntryResponse>>
{
    public ValueTask<List<Big3LeaderboardEntryResponse>> Handle(
        GetBig3LeaderboardQuery request, CancellationToken cancellationToken)
    {
        var gender = request.Gender?.ToLowerInvariant() switch
        {
            "male" => (Gender?)Gender.Male,
            "female" => (Gender?)Gender.Female,
            _ => null,
        };

        return new ValueTask<List<Big3LeaderboardEntryResponse>>(cache is null
            ? leaderboardRepo.GetBig3Async(gender, cancellationToken)
            : cache.GetOrCreateAsync(
                CacheTags.Leaderboards,
                $"big3:gender:{request.Gender?.ToLowerInvariant() ?? "all"}",
                TimeSpan.FromSeconds(30),
                ct => leaderboardRepo.GetBig3Async(gender, ct),
                cancellationToken));
    }
}
