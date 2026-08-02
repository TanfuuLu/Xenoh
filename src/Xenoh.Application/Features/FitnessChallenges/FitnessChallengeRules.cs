using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

internal static class FitnessChallengeRules
{
    private static readonly string[] LocalDateTimeFormats =
        ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"];

    private static readonly FitnessChallengeMemberStatus[] ReservedStatuses =
        [FitnessChallengeMemberStatus.Invited, FitnessChallengeMemberStatus.Accepted];

    public static FitnessChallengeStatus Status(FitnessChallenge challenge, DateTime nowUtc) =>
        challenge.CancelledAt.HasValue ? FitnessChallengeStatus.Cancelled :
        nowUtc < challenge.StartsAtUtc ? FitnessChallengeStatus.Upcoming :
        nowUtc <= challenge.EndsAtUtc ? FitnessChallengeStatus.Active :
        FitnessChallengeStatus.Completed;

    public static int ReservedCount(FitnessChallenge challenge) =>
        challenge.Members.Count(x => ReservedStatuses.Contains(x.Status));

    public static int AcceptedCount(FitnessChallenge challenge) =>
        challenge.Members.Count(x => x.Status == FitnessChallengeMemberStatus.Accepted);

    public static IQueryable<FitnessChallenge> IncludeAll(IApplicationDbContext db) =>
        db.FitnessChallenges
            .Include(x => x.Creator)
            .Include(x => x.Members).ThenInclude(x => x.User);

    public static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { throw new InvalidOperationException("Unknown challenge timezone."); }
        catch (InvalidTimeZoneException) { throw new InvalidOperationException("Unknown challenge timezone."); }
    }

    public static DateOnly LocalDate(FitnessChallenge challenge, DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            GetTimeZone(challenge.TimeZoneId)));

    public static (DateOnly Start, DateOnly EndExclusive) LocalDateRange(FitnessChallenge challenge) =>
        (LocalDate(challenge, challenge.StartsAtUtc), LocalDate(challenge, challenge.EndsAtUtc));

    public static async Task<HashSet<Guid>> BlockedIdsAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct) =>
        (await db.UserBlocks.AsNoTracking()
            .Where(x => x.BlockerId == userId || x.BlockedId == userId)
            .Select(x => x.BlockerId == userId ? x.BlockedId : x.BlockerId)
            .ToListAsync(ct))
        .ToHashSet();

    public static async Task<HashSet<Guid>> ConnectionIdsAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct)
    {
        var friendIds = await db.Friendships.AsNoTracking()
            .Where(x => x.Status == FriendshipStatus.Accepted &&
                        (x.UserAId == userId || x.UserBId == userId))
            .Select(x => x.UserAId == userId ? x.UserBId : x.UserAId)
            .ToListAsync(ct);
        var clientIds = await db.CoachClientRelationships.AsNoTracking()
            .Where(x => x.Status == RelationshipStatus.Active &&
                        (x.CoachId == userId || x.ClientId == userId))
            .Select(x => x.CoachId == userId ? x.ClientId : x.CoachId)
            .ToListAsync(ct);
        return friendIds.Concat(clientIds).ToHashSet();
    }

    public static async Task<bool> CanDiscoverAsync(
        IApplicationDbContext db,
        FitnessChallenge challenge,
        Guid userId,
        CancellationToken ct)
    {
        if (challenge.CreatorId == userId || challenge.AccessType == FitnessChallengeAccessType.Community)
            return !(await BlockedIdsAsync(db, userId, ct)).Contains(challenge.CreatorId);
        if (challenge.AccessType != FitnessChallengeAccessType.Connections) return false;
        var blocked = await BlockedIdsAsync(db, userId, ct);
        if (blocked.Contains(challenge.CreatorId)) return false;
        return (await ConnectionIdsAsync(db, userId, ct)).Contains(challenge.CreatorId);
    }

    public static async Task EnsureInviteesEligibleAsync(
        IApplicationDbContext db,
        Guid creatorId,
        IReadOnlyCollection<Guid> inviteeIds,
        CancellationToken ct)
    {
        if (inviteeIds.Count == 0) return;
        var blocked = await BlockedIdsAsync(db, creatorId, ct);
        var connections = await ConnectionIdsAsync(db, creatorId, ct);
        if (inviteeIds.Any(x => x == creatorId || blocked.Contains(x) || !connections.Contains(x)))
            throw new InvalidOperationException("Invitees must be unblocked accepted friends or active coach/client connections.");
    }

    public static async Task<(bool IsPaid, int MaxWeeks, int MaxMembers, int MaxOwned)> LimitsAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct)
    {
        var subscription = await db.UserSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var isPaid = subscription is { Tier: not PlanTier.Free } && subscription.IsActive;
        return isPaid ? (true, 12, 25, 5) : (false, 4, 10, 1);
    }

    public static FitnessChallengeInput ValidateInput(
        FitnessChallengeInput input,
        DateTime nowUtc,
        int maxWeeks,
        int maxMembers)
    {
        var title = input.Title.Trim();
        var description = input.Description.Trim();
        var checkInPrompt = input.CheckInPrompt?.Trim();
        if (title.Length is < 3 or > 80) throw new InvalidOperationException("Title must contain 3 to 80 characters.");
        if (description.Length > 1000) throw new InvalidOperationException("Description must contain at most 1,000 characters.");
        if (!Enum.IsDefined(input.MetricType))
            throw new InvalidOperationException("Unknown challenge metric type.");
        if (!Enum.IsDefined(input.AccessType))
            throw new InvalidOperationException("Unknown challenge access type.");
        if (input.SelectedLifts.Any(lift => !Enum.IsDefined(lift)))
            throw new InvalidOperationException("Unknown competition lift.");
        var timeZone = GetTimeZone(input.TimeZoneId);
        var hasLocalStart = !string.IsNullOrWhiteSpace(input.StartsAtLocal);
        var hasLocalEnd = !string.IsNullOrWhiteSpace(input.EndsAtLocal);
        if (hasLocalStart != hasLocalEnd)
            throw new InvalidOperationException("Both local challenge schedule values are required.");
        var startsAtUtc = hasLocalStart
            ? ConvertLocalToUtc(input.StartsAtLocal!, timeZone, "start")
            : DateTime.SpecifyKind(input.StartsAtUtc.ToUniversalTime(), DateTimeKind.Utc);
        var endsAtUtc = hasLocalEnd
            ? ConvertLocalToUtc(input.EndsAtLocal!, timeZone, "end")
            : DateTime.SpecifyKind(input.EndsAtUtc.ToUniversalTime(), DateTimeKind.Utc);
        if (startsAtUtc <= nowUtc || startsAtUtc > nowUtc.AddDays(28))
            throw new InvalidOperationException("Challenge must start within the next 28 days.");
        if (endsAtUtc <= startsAtUtc || endsAtUtc - startsAtUtc < TimeSpan.FromDays(1))
            throw new InvalidOperationException("Challenge must last at least one day.");
        if (endsAtUtc - startsAtUtc > TimeSpan.FromDays(maxWeeks * 7))
            throw new InvalidOperationException($"Your plan supports challenges up to {maxWeeks} weeks.");
        if (input.Capacity is < 2 || input.Capacity > maxMembers)
            throw new InvalidOperationException($"Your plan supports between 2 and {maxMembers} participants.");
        if (input.MetricType == FitnessChallengeMetricType.TrainingSessions &&
            input.TargetSessionsPerWeek is < 1 or > 7)
            throw new InvalidOperationException("Weekly target must be between 1 and 7.");
        var selectedLifts = input.SelectedLifts.Distinct().ToList();
        if (input.MetricType == FitnessChallengeMetricType.SbdImprovement && selectedLifts.Count == 0)
            throw new InvalidOperationException("Select at least one competition lift.");
        if (input.MetricType == FitnessChallengeMetricType.CustomCheckIns &&
            (checkInPrompt?.Length ?? 0) is < 3 or > 160)
            throw new InvalidOperationException("Custom challenges require a check-in prompt of 3 to 160 characters.");
        return input with
        {
            Title = title,
            Description = description,
            CheckInPrompt = input.MetricType == FitnessChallengeMetricType.CustomCheckIns ? checkInPrompt : null,
            TargetSessionsPerWeek = input.MetricType == FitnessChallengeMetricType.TrainingSessions
                ? input.TargetSessionsPerWeek
                : 0,
            SelectedLifts = input.MetricType == FitnessChallengeMetricType.SbdImprovement ? selectedLifts : [],
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            StartsAtLocal = hasLocalStart ? input.StartsAtLocal!.Trim() : null,
            EndsAtLocal = hasLocalEnd ? input.EndsAtLocal!.Trim() : null,
            InviteeUserIds = input.InviteeUserIds.Distinct().ToList()
        };
    }

    private static DateTime ConvertLocalToUtc(
        string value,
        TimeZoneInfo timeZone,
        string fieldName)
    {
        if (!DateTime.TryParseExact(
                value.Trim(),
                LocalDateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            throw new InvalidOperationException(
                $"Challenge {fieldName} time must use yyyy-MM-ddTHH:mm format.");

        var local = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(local))
            throw new InvalidOperationException(
                $"Challenge {fieldName} time does not exist in the selected timezone.");
        if (timeZone.IsAmbiguousTime(local))
            throw new InvalidOperationException(
                $"Challenge {fieldName} time is ambiguous in the selected timezone.");

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    public static string ScoreUnit(FitnessChallengeMetricType metricType) => metricType switch
    {
        FitnessChallengeMetricType.TrainingSessions => "sessions",
        FitnessChallengeMetricType.TrainingStreak => "days",
        FitnessChallengeMetricType.SbdImprovement => "%",
        FitnessChallengeMetricType.CustomCheckIns => "check-ins",
        _ => "points"
    };

    public static decimal? SbdImprovementScore(
        FitnessChallenge challenge,
        Guid userId,
        IReadOnlyList<PrMetricRow> prRows,
        DateTime scoreAtUtc,
        out bool baselineReady)
    {
        var baselineTotal = 0m;
        var currentTotal = 0m;
        foreach (var lift in challenge.SelectedLifts)
        {
            var liftRows = prRows.Where(x => x.UserId == userId && x.Lift == lift).ToList();
            var baseline = liftRows.Where(x => x.AchievedAt < challenge.StartsAtUtc)
                .Select(x => OneRepMaxCalculator.Estimate(x.Weight, x.Reps) ?? 0m)
                .DefaultIfEmpty().Max();
            if (baseline <= 0m)
            {
                baselineReady = false;
                return null;
            }
            var current = liftRows.Where(x => x.AchievedAt <= scoreAtUtc)
                .Select(x => OneRepMaxCalculator.Estimate(x.Weight, x.Reps) ?? 0m)
                .DefaultIfEmpty(baseline).Max();
            baselineTotal += baseline;
            currentTotal += current;
        }
        baselineReady = true;
        return baselineTotal <= 0m
            ? null
            : Math.Round(Math.Max(0m, (currentTotal - baselineTotal) / baselineTotal * 100m), 2);
    }

    public static int LongestStreak(IEnumerable<DateOnly> completedDates)
    {
        var dates = completedDates.Distinct().Order().ToList();
        var longest = 0;
        var current = 0;
        DateOnly? previous = null;
        foreach (var date in dates)
        {
            current = previous.HasValue && previous.Value.AddDays(1) == date ? current + 1 : 1;
            longest = Math.Max(longest, current);
            previous = date;
        }
        return longest;
    }
}

internal sealed record WorkoutMetricRow(Guid UserId, DateOnly Date);
internal sealed record PrMetricRow(
    Guid UserId,
    CompetitionLiftType Lift,
    decimal Weight,
    int Reps,
    DateTime AchievedAt);
