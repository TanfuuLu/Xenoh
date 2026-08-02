using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

internal static class FitnessChallengeMapping
{
    public static async Task<IReadOnlyList<FitnessChallengeResponse>> MapManyAsync(
        IApplicationDbContext db,
        IReadOnlyList<FitnessChallenge> challenges,
        Guid currentUserId,
        bool exposeNonMemberDetails,
        CancellationToken ct)
    {
        if (challenges.Count == 0) return [];
        var now = DateTime.UtcNow;
        var acceptedUserIds = challenges
            .SelectMany(x => x.Members)
            .Where(x => x.Status == FitnessChallengeMemberStatus.Accepted)
            .Select(x => x.UserId)
            .Distinct()
            .ToList();
        var dateRanges = challenges.Select(FitnessChallengeRules.LocalDateRange).ToList();
        var minDate = dateRanges.Min(x => x.Start);
        var maxDateExclusive = dateRanges.Max(x => x.EndExclusive);
        var workoutRows = acceptedUserIds.Count == 0
            ? []
            : await db.DailyWorkouts.AsNoTracking()
                .Where(x => acceptedUserIds.Contains(x.WeeklyWorkout.Plan.OwnerId) &&
                            x.IsCompleted && x.Date >= minDate && x.Date < maxDateExclusive)
                .Select(x => new WorkoutMetricRow(x.WeeklyWorkout.Plan.OwnerId, x.Date))
                .Distinct()
                .ToListAsync(ct);
        var challengeIds = challenges.Select(x => x.Id).ToList();
        var checkIns = await db.FitnessChallengeCheckIns.AsNoTracking()
            .Where(x => challengeIds.Contains(x.ChallengeId))
            .Select(x => new { x.ChallengeId, x.UserId, x.LocalDate })
            .ToListAsync(ct);
        var maxEnd = challenges.Max(x => x.EndsAtUtc);
        var prRows = acceptedUserIds.Count == 0
            ? []
            : await (
                from history in db.UserExercisePRHistories.AsNoTracking()
                join template in db.ExerciseTemplates.AsNoTracking()
                    on history.ExerciseTemplateId equals template.Id
                where acceptedUserIds.Contains(history.UserId) &&
                      history.AchievedAt <= maxEnd &&
                      template.CompetitionLiftType != null
                select new PrMetricRow(
                    history.UserId,
                    template.CompetitionLiftType!.Value,
                    history.Weight,
                    history.Reps,
                    history.AchievedAt))
                .ToListAsync(ct);

        var results = new List<FitnessChallengeResponse>(challenges.Count);
        foreach (var challenge in challenges)
        {
            var isVisibleMember = challenge.CreatorId == currentUserId ||
                challenge.Members.Any(x => x.UserId == currentUserId &&
                    x.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited);
            var exposeMembers = exposeNonMemberDetails || isVisibleMember;
            var range = FitnessChallengeRules.LocalDateRange(challenge);
            var scoreAt = now < challenge.EndsAtUtc ? now : challenge.EndsAtUtc;
            var todayLocal = FitnessChallengeRules.LocalDate(challenge, now);
            var rawMembers = new List<RawMemberScore>();
            foreach (var member in challenge.Members.Where(x => x.Status == FitnessChallengeMemberStatus.Accepted))
            {
                var dates = workoutRows.Where(x => x.UserId == member.UserId &&
                                                   x.Date >= range.Start && x.Date < range.EndExclusive)
                    .Select(x => x.Date)
                    .Distinct()
                    .ToList();
                bool baselineReady;
                decimal? score = challenge.MetricType switch
                {
                    FitnessChallengeMetricType.TrainingSessions => dates.Count,
                    FitnessChallengeMetricType.TrainingStreak => FitnessChallengeRules.LongestStreak(dates),
                    FitnessChallengeMetricType.SbdImprovement =>
                        FitnessChallengeRules.SbdImprovementScore(challenge, member.UserId, prRows, scoreAt, out baselineReady),
                    FitnessChallengeMetricType.CustomCheckIns =>
                        checkIns.Count(x => x.ChallengeId == challenge.Id && x.UserId == member.UserId &&
                                            x.LocalDate >= range.Start && x.LocalDate < range.EndExclusive),
                    _ => 0m
                };
                baselineReady = challenge.MetricType != FitnessChallengeMetricType.SbdImprovement || score.HasValue;
                rawMembers.Add(new RawMemberScore(member, dates, score, baselineReady));
            }

            var rank = 0;
            var position = 0;
            decimal? previousScore = null;
            foreach (var item in rawMembers.Where(x => x.Score.HasValue)
                         .OrderByDescending(x => x.Score).ThenBy(x => x.Member.CreatedAt))
            {
                position++;
                if (!previousScore.HasValue || item.Score != previousScore) rank = position;
                item.Rank = rank;
                previousScore = item.Score;
            }

            var memberResponses = exposeMembers
                ? challenge.Members
                    .OrderBy(x => x.Status == FitnessChallengeMemberStatus.Accepted ? 0 : 1)
                    .ThenBy(x => rawMembers.FirstOrDefault(r => r.Member.Id == x.Id)?.Rank ?? int.MaxValue)
                    .ThenByDescending(x => x.UserId == challenge.CreatorId)
                    .Select(member =>
                    {
                        var raw = rawMembers.FirstOrDefault(x => x.Member.Id == member.Id);
                        var weeks = raw is null ? [] : BuildWeeks(challenge, raw.CompletedDates, range);
                        return new ChallengeMemberResponse(
                            member.UserId,
                            $"{member.User.FirstName} {member.User.LastName}".Trim(),
                            member.User.AvatarUrl,
                            member.Status.ToString(),
                            member.UserId == challenge.CreatorId,
                            raw?.Score,
                            raw?.Rank,
                            FitnessChallengeRules.ScoreUnit(challenge.MetricType),
                            raw?.BaselineReady ?? true,
                            checkIns.Any(x => x.ChallengeId == challenge.Id &&
                                              x.UserId == member.UserId &&
                                              x.LocalDate == todayLocal),
                            raw?.CompletedDates.Count ?? 0,
                            weeks.Sum(x => x.TargetSessions),
                            weeks);
                    })
                    .ToList()
                : [];

            var status = FitnessChallengeRules.Status(challenge, now);
            var currentMember = challenge.Members.FirstOrDefault(x => x.UserId == currentUserId);
            var canJoin = status == FitnessChallengeStatus.Upcoming &&
                          FitnessChallengeRules.ReservedCount(challenge) < challenge.Capacity &&
                          currentMember?.Status != FitnessChallengeMemberStatus.Removed &&
                          currentMember?.Status != FitnessChallengeMemberStatus.Accepted &&
                          currentMember?.Status != FitnessChallengeMemberStatus.Invited &&
                          challenge.AccessType != FitnessChallengeAccessType.InviteOnly;
            results.Add(new FitnessChallengeResponse(
                challenge.Id,
                challenge.Title,
                challenge.Description,
                challenge.CreatorId,
                $"{challenge.Creator.FirstName} {challenge.Creator.LastName}".Trim(),
                challenge.MetricType,
                challenge.AccessType,
                challenge.TargetSessionsPerWeek,
                challenge.SelectedLifts,
                challenge.CheckInPrompt,
                challenge.Capacity,
                FitnessChallengeRules.AcceptedCount(challenge),
                FitnessChallengeRules.ReservedCount(challenge),
                challenge.TimeZoneId,
                challenge.StartsAtUtc,
                challenge.EndsAtUtc,
                status.ToString(),
                challenge.CreatorId == currentUserId,
                canJoin,
                now >= challenge.StartsAtUtc,
                memberResponses));
        }
        return results;
    }

    public static FitnessChallengeSummaryResponse Summary(
        FitnessChallenge challenge,
        Guid currentUserId,
        DateTime nowUtc)
    {
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUserId);
        var canJoin = FitnessChallengeRules.Status(challenge, nowUtc) == FitnessChallengeStatus.Upcoming &&
                      FitnessChallengeRules.ReservedCount(challenge) < challenge.Capacity &&
                      member?.Status != FitnessChallengeMemberStatus.Removed &&
                      member?.Status != FitnessChallengeMemberStatus.Accepted &&
                      member?.Status != FitnessChallengeMemberStatus.Invited &&
                      challenge.AccessType != FitnessChallengeAccessType.InviteOnly;
        return new FitnessChallengeSummaryResponse(
            challenge.Id,
            challenge.Title,
            challenge.Description,
            challenge.CreatorId,
            $"{challenge.Creator.FirstName} {challenge.Creator.LastName}".Trim(),
            challenge.Creator.AvatarUrl,
            challenge.MetricType,
            challenge.AccessType,
            challenge.Capacity,
            FitnessChallengeRules.AcceptedCount(challenge),
            FitnessChallengeRules.ReservedCount(challenge),
            challenge.TimeZoneId,
            challenge.StartsAtUtc,
            challenge.EndsAtUtc,
            FitnessChallengeRules.Status(challenge, nowUtc).ToString(),
            canJoin);
    }

    private static IReadOnlyList<ChallengeWeekProgressResponse> BuildWeeks(
        FitnessChallenge challenge,
        IReadOnlyCollection<DateOnly> completedDates,
        (DateOnly Start, DateOnly EndExclusive) range)
    {
        if (challenge.MetricType != FitnessChallengeMetricType.TrainingSessions) return [];
        var weeks = new List<ChallengeWeekProgressResponse>();
        for (var start = range.Start; start < range.EndExclusive; start = start.AddDays(7))
        {
            var endExclusive = start.AddDays(7) < range.EndExclusive
                ? start.AddDays(7)
                : range.EndExclusive;
            var coveredDays = endExclusive.DayNumber - start.DayNumber;
            var target = (int)Math.Ceiling(
                challenge.TargetSessionsPerWeek * coveredDays / 7m);
            weeks.Add(new ChallengeWeekProgressResponse(
                start,
                endExclusive.AddDays(-1),
                completedDates.Count(x => x >= start && x < endExclusive),
                target));
        }
        return weeks;
    }

    private sealed class RawMemberScore(
        FitnessChallengeMember member,
        IReadOnlyList<DateOnly> completedDates,
        decimal? score,
        bool baselineReady)
    {
        public FitnessChallengeMember Member { get; } = member;
        public IReadOnlyList<DateOnly> CompletedDates { get; } = completedDates;
        public decimal? Score { get; } = score;
        public bool BaselineReady { get; } = baselineReady;
        public int? Rank { get; set; }
    }
}
