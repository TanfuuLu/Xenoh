using Mediator;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;

public sealed class GetLeaderboardHandler(ILeaderboardRepository leaderboardRepo)
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryResponse>>
{
    private const int MaxEntries = 50;

    public async ValueTask<List<LeaderboardEntryResponse>> Handle(
        GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var type = request.Type.ToLowerInvariant();
        var gender = ParseGender(request.Gender);

        if (type is "squat" or "bench" or "deadlift")
        {
            var lift = type switch
            {
                "squat" => CompetitionLiftType.Squat,
                "bench" => CompetitionLiftType.Bench,
                _ => CompetitionLiftType.Deadlift
            };
            return await leaderboardRepo.GetSingleLiftAsync(lift, gender, MaxEntries, cancellationToken);
        }

        return await leaderboardRepo.GetDotsAsync(gender, MaxEntries, cancellationToken);
    }

    private static Gender? ParseGender(string? gender) => gender?.ToLowerInvariant() switch
    {
        "male" => Gender.Male,
        "female" => Gender.Female,
        _ => null
    };
}
