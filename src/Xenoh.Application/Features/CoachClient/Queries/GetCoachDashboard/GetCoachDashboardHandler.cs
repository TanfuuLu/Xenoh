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
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IApplicationCache? cache = null
) : IRequestHandler<GetCoachDashboardQuery, List<CoachClientDashboardResponse>>
{
    public ValueTask<List<CoachClientDashboardResponse>> Handle(
        GetCoachDashboardQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        return new(cache is null
            ? BuildAsync(coachId, cancellationToken)
            : cache.GetOrCreateAsync(CacheTags.CoachDashboards, $"coach:{coachId:N}", TimeSpan.FromSeconds(15),
                ct => BuildAsync(coachId, ct), cancellationToken));
    }

    private async Task<List<CoachClientDashboardResponse>> BuildAsync(
        Guid coachId, CancellationToken cancellationToken)
    {

        var allClients = await coachClientRepo.GetAllByCoachAsync(coachId, cancellationToken);
        var activeClients = allClients
            .Where(r => r.Status == RelationshipStatus.Active.ToString())
            .ToList();

        if (activeClients.Count == 0)
            return [];

        var clientIds = activeClients.Select(r => r.ClientId).ToList();

        var lastWorkoutDates = await workoutHistoryRepo.GetLastDatesForUsersAsync(clientIds, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // All coach-facing client metrics are scoped to the plan this coach authored.
        var planMonitoring = await planRepo.GetMonitoringByOwnersAsync(clientIds, coachId, today, cancellationToken);
        var latestCompletedWorkoutDates = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d =>
                d.IsCompleted &&
                d.Date <= today &&
                d.WeeklyWorkout.Plan.CreatedByCoachId == coachId &&
                clientIds.Contains(d.WeeklyWorkout.Plan.OwnerId))
            .GroupBy(d => d.WeeklyWorkout.Plan.OwnerId)
            .Select(g => new { OwnerId = g.Key, Date = g.Max(d => d.Date) })
            .ToDictionaryAsync(x => x.OwnerId, x => (DateOnly?)x.Date, cancellationToken);
        var latestBodyweights = await bodyweightRepo.GetLatestWeightsForUsersAsync(clientIds, cancellationToken);
        var competitionLifts = await userPrRepo.GetCompetitionLiftsForUsersAsync(clientIds, cancellationToken);

        var clientIdStrings = clientIds.Select(id => id.ToString()).ToList();
        var avatarUrls = await userManager.Users
            .AsNoTracking()
            .Where(u => clientIdStrings.Contains(u.Id.ToString()))
            .Select(u => new { u.Id, u.AvatarUrl })
            .ToDictionaryAsync(u => Guid.Parse(u.Id.ToString()), u => u.AvatarUrl, cancellationToken);

        var prsByClient = competitionLifts
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.LiftType, x => (decimal?)x.Weight));

        var monitoringByClient = planMonitoring.ToDictionary(p => p.OwnerId);

        return activeClients.Select(r =>
        {
            var clientPrs = prsByClient.GetValueOrDefault(r.ClientId, []);
            var monitoring = monitoringByClient.GetValueOrDefault(r.ClientId);
            var lastWorkoutDate = lastWorkoutDates.GetValueOrDefault(r.ClientId);
            var latestCompletedWorkoutDate = latestCompletedWorkoutDates.GetValueOrDefault(r.ClientId);
            int? daysSinceLastWorkout = lastWorkoutDate is null
                ? null
                : today.DayNumber - lastWorkoutDate.Value.DayNumber;
            var attention = CoachAttentionAnalyzer.Analyze(monitoring, daysSinceLastWorkout);

            return new CoachClientDashboardResponse(
                r.ClientId,
                r.FullName,
                r.Email,
                avatarUrls.GetValueOrDefault(r.ClientId),
                lastWorkoutDate,
                monitoring?.ActivePlanProgressPercent,
                latestBodyweights.GetValueOrDefault(r.ClientId),
                new BigThreePRs(
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Squat),
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Bench),
                    clientPrs.GetValueOrDefault(CompetitionLiftType.Deadlift)
                ),
                monitoring?.ActivePlanId,
                monitoring?.ActivePlanName,
                monitoring?.ActivePlanStartDate,
                monitoring?.ActivePlanEndDate,
                monitoring?.ActivePlanProgressPercent,
                daysSinceLastWorkout,
                latestCompletedWorkoutDate,
                latestCompletedWorkoutDate == today,
                monitoring?.CompletedWorkoutDays ?? 0,
                monitoring?.TotalWorkoutDays ?? 0,
                monitoring?.MissedWorkoutDays ?? 0,
                monitoring?.CompletedWorkoutDays ?? 0,
                monitoring?.TotalWorkoutDays ?? 0,
                attention.Level,
                attention.Reasons
            );
        }).ToList();
    }
}
