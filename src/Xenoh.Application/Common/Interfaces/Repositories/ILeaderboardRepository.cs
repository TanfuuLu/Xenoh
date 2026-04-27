using Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;
using Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface ILeaderboardRepository
{
    Task<List<LeaderboardEntryResponse>> GetSingleLiftAsync(CompetitionLiftType lift, Gender? gender, int max, CancellationToken ct = default);
    Task<List<LeaderboardEntryResponse>> GetDotsAsync(Gender? gender, int max, CancellationToken ct = default);
    Task<List<Big3LeaderboardEntryResponse>> GetBig3Async(Gender? gender, CancellationToken ct = default);
}
