using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;

public sealed class GetCoachDashboardHandler(
    ICoachClientRepository coachClientRepo,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IPlanRepository planRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetCoachDashboardQuery, List<CoachClientDashboardResponse>>
{
    public async ValueTask<List<CoachClientDashboardResponse>> Handle(
        GetCoachDashboardQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var allClients = await coachClientRepo.GetAllByCoachAsync(coachId, cancellationToken);
        var activeClients = allClients
            .Where(r => r.Status == RelationshipStatus.Active.ToString())
            .ToList();

        if (activeClients.Count == 0)
            return [];

        var clientIds = activeClients.Select(r => r.ClientId).ToList();

        var lastWorkoutDates = await workoutHistoryRepo.GetLastDatesForUsersAsync(clientIds, cancellationToken);
        var planProgress = await planRepo.GetProgressByOwnersAsync(clientIds, cancellationToken);
        var latestBodyweights = await bodyweightRepo.GetLatestWeightsForUsersAsync(clientIds, cancellationToken);
        var competitionLifts = await userPrRepo.GetCompetitionLiftsForUsersAsync(clientIds, cancellationToken);

        // Batch-fetch avatar URLs for all clients in one query
        var clientIdStrings = clientIds.Select(id => id.ToString()).ToList();
        var avatarUrls = await userManager.Users
            .Where(u => clientIdStrings.Contains(u.Id.ToString()))
            .Select(u => new { u.Id, u.AvatarUrl })
            .ToDictionaryAsync(u => Guid.Parse(u.Id.ToString()), u => u.AvatarUrl, cancellationToken);

        // Aggregate plan progress per client
        var progressByClient = planProgress
            .GroupBy(p => p.OwnerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    int total = g.Sum(p => p.TotalDays);
                    int completed = g.Sum(p => p.CompletedDays);
                    return total > 0 ? (int?)Math.Round(completed * 100.0 / total) : null;
                });

        // Group Big3 PRs per client
        var prsByClient = competitionLifts
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.LiftType, x => (decimal?)x.Weight));

        return activeClients.Select(r =>
        {
            var clientPrs = prsByClient.GetValueOrDefault(r.ClientId, []);
            return new CoachClientDashboardResponse(
                r.ClientId,
                r.FullName,
                r.Email,
                avatarUrls.GetValueOrDefault(r.ClientId),
                lastWorkoutDates.GetValueOrDefault(r.ClientId),
                progressByClient.GetValueOrDefault(r.ClientId),
                latestBodyweights.GetValueOrDefault(r.ClientId),
                new BigThreePRs(
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Squat),
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Bench),
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Deadlift)
                )
            );
        }).ToList();
    }
}
