using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

public sealed record ProcessFitnessChallengeNotificationsCommand : IRequest<int>;

public sealed class ProcessFitnessChallengeNotificationsHandler(IApplicationDbContext db, INotificationService notifications)
    : IRequestHandler<ProcessFitnessChallengeNotificationsCommand, int>
{
    public async ValueTask<int> Handle(ProcessFitnessChallengeNotificationsCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenges = await db.FitnessChallenges.Include(x => x.Members)
            .Where(x => x.CancelledAt == null && x.StartsAtUtc <= now && x.CompletionNotifiedAt == null)
            .ToListAsync(ct);
        var calls = new List<(Guid UserId, string Type, string Message, Guid ChallengeId)>();
        foreach (var challenge in challenges)
        {
            var accepted = challenge.Members.Where(x => x.Status == FitnessChallengeMemberStatus.Accepted).ToList();
            if (now > challenge.EndsAtUtc)
            {
                challenge.Status = FitnessChallengeStatus.Completed;
                challenge.CompletionNotifiedAt = DateTime.UtcNow;
                calls.AddRange(accepted.Select(x => (x.UserId, "FitnessChallengeCompleted", $"{challenge.Title} is complete. Review the final leaderboard.", challenge.Id)));
                continue;
            }

            challenge.Status = FitnessChallengeStatus.Active;
            if (!challenge.StartNotifiedAt.HasValue)
            {
                challenge.StartNotifiedAt = now;
                calls.AddRange(accepted.Select(x => (x.UserId, "FitnessChallengeStarted", $"{challenge.Title} has started. Enrollment is now closed.", challenge.Id)));
            }
            if (challenge.MetricType != FitnessChallengeMetricType.TrainingSessions) continue;
            var localToday = FitnessChallengeRules.LocalDate(challenge, now);
            var localStart = FitnessChallengeRules.LocalDate(challenge, challenge.StartsAtUtc);
            var weekStart = localStart.AddDays(((localToday.DayNumber - localStart.DayNumber) / 7) * 7);
            foreach (var member in accepted)
            {
                var completed = await db.DailyWorkouts.AsNoTracking().Where(x => x.WeeklyWorkout.Plan.OwnerId == member.UserId &&
                    x.IsCompleted && x.Date >= weekStart && x.Date <= weekStart.AddDays(6)).Select(x => x.Date).Distinct().CountAsync(ct);
                if (completed >= challenge.TargetSessionsPerWeek && member.LastCompletionNotificationWeekStart != weekStart)
                {
                    member.LastCompletionNotificationWeekStart = weekStart;
                    calls.Add((member.UserId, "FitnessChallengeTargetCompleted", $"Weekly target completed in {challenge.Title}.", challenge.Id));
                }
                else if ((localToday.DayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday) &&
                         member.LastBehindReminderWeekStart != weekStart)
                {
                    member.LastBehindReminderWeekStart = weekStart;
                    calls.Add((member.UserId, "FitnessChallengeReminder", $"{challenge.TargetSessionsPerWeek - completed} session(s) remain for this week in {challenge.Title}.", challenge.Id));
                }
            }
        }
        await db.SaveChangesAsync(ct);
        foreach (var call in calls)
            await notifications.NotifyAsync(call.UserId, call.Type, call.Message, call.ChallengeId, "FitnessChallenge", ct);
        return calls.Count;
    }
}
