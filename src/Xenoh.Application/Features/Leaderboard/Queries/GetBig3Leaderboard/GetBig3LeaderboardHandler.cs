using Mediator;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;

public sealed class GetBig3LeaderboardHandler(ILeaderboardRepository leaderboardRepo)
    : IRequestHandler<GetBig3LeaderboardQuery, List<Big3LeaderboardEntryResponse>>
{
    public async ValueTask<List<Big3LeaderboardEntryResponse>> Handle(
        GetBig3LeaderboardQuery request, CancellationToken cancellationToken)
    {
        var gender = request.Gender?.ToLowerInvariant() switch
        {
            "male"   => (Gender?)Gender.Male,
            "female" => (Gender?)Gender.Female,
            _        => null,
        };

        return await leaderboardRepo.GetBig3Async(gender, cancellationToken);
    }
}
