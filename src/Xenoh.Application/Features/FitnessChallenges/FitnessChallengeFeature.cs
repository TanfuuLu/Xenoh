using System.ComponentModel.DataAnnotations;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

public sealed record ChallengeWeekProgressResponse(DateOnly StartsOn, DateOnly EndsOn, int CompletedSessions, int TargetSessions);
public sealed record ChallengeMemberResponse(
    Guid UserId, string FullName, string? AvatarUrl, string Status, bool IsCreator,
    int CompletedSessions, int TargetSessions, IReadOnlyList<ChallengeWeekProgressResponse> Weeks);
public sealed record FitnessChallengeResponse(
    Guid Id, string Title, Guid CreatorId, int TargetSessionsPerWeek, DateOnly StartsOn, DateOnly EndsOn,
    string Status, bool CanManage, IReadOnlyList<ChallengeMemberResponse> Members);
public sealed record ChallengeInviteeResponse(Guid UserId, string FullName, string? AvatarUrl, string Relationship);

public sealed record CreateFitnessChallengeCommand : IRequest<FitnessChallengeResponse>
{
    [Required, MinLength(3), MaxLength(80)] public string Title { get; init; } = string.Empty;
    [Range(1, 7)] public int TargetSessionsPerWeek { get; init; }
    public DateOnly StartsOn { get; init; }
    [Range(1, 12)] public int DurationWeeks { get; init; }
    public IReadOnlyList<Guid> InviteeUserIds { get; init; } = [];
}
public sealed record GetFitnessChallengesQuery(string? Status = null) : IRequest<IReadOnlyList<FitnessChallengeResponse>>;
public sealed record GetFitnessChallengeQuery(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
public sealed record GetChallengeInviteesQuery : IRequest<IReadOnlyList<ChallengeInviteeResponse>>;
public sealed record AcceptFitnessChallengeCommand(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
public sealed record DeclineFitnessChallengeCommand(Guid ChallengeId) : IRequest;
public sealed record LeaveFitnessChallengeCommand(Guid ChallengeId) : IRequest;
public sealed record CancelFitnessChallengeCommand(Guid ChallengeId) : IRequest;

internal static class FitnessChallengeRules
{
    public static FitnessChallengeStatus Status(FitnessChallenge challenge, DateOnly today) =>
        challenge.CancelledAt.HasValue ? FitnessChallengeStatus.Cancelled :
        today < challenge.StartsOn ? FitnessChallengeStatus.Upcoming :
        today <= challenge.EndsOn ? FitnessChallengeStatus.Active : FitnessChallengeStatus.Completed;

    public static async Task<FitnessChallengeResponse> MapAsync(
        IApplicationDbContext db, FitnessChallenge challenge, Guid currentUserId, CancellationToken ct)
    {
        var acceptedIds = challenge.Members.Where(x => x.Status == FitnessChallengeMemberStatus.Accepted)
            .Select(x => x.UserId).ToList();
        var completedDates = await db.DailyWorkouts.AsNoTracking()
            .Where(x => acceptedIds.Contains(x.WeeklyWorkout.Plan.OwnerId) && x.IsCompleted &&
                        x.Date >= challenge.StartsOn && x.Date <= challenge.EndsOn)
            .Select(x => new { UserId = x.WeeklyWorkout.Plan.OwnerId, x.Date })
            .Distinct().ToListAsync(ct);

        var weeks = Enumerable.Range(0, ((challenge.EndsOn.DayNumber - challenge.StartsOn.DayNumber) / 7) + 1)
            .Select(i => challenge.StartsOn.AddDays(i * 7)).ToList();
        var members = challenge.Members.OrderByDescending(x => x.UserId == challenge.CreatorId)
            .ThenBy(x => x.User.FirstName).ThenBy(x => x.User.LastName).Select(member =>
            {
                var memberDates = completedDates.Where(x => x.UserId == member.UserId).Select(x => x.Date).ToHashSet();
                var progress = weeks.Select(start => new ChallengeWeekProgressResponse(
                    start, start.AddDays(6), memberDates.Count(x => x >= start && x <= start.AddDays(6)),
                    challenge.TargetSessionsPerWeek)).ToList();
                return new ChallengeMemberResponse(member.UserId,
                    $"{member.User.FirstName} {member.User.LastName}".Trim(), member.User.AvatarUrl,
                    member.Status.ToString(), member.UserId == challenge.CreatorId,
                    progress.Sum(x => x.CompletedSessions), progress.Sum(x => x.TargetSessions), progress);
            }).ToList();

        return new FitnessChallengeResponse(challenge.Id, challenge.Title, challenge.CreatorId,
            challenge.TargetSessionsPerWeek, challenge.StartsOn, challenge.EndsOn,
            Status(challenge, DateOnly.FromDateTime(DateTime.UtcNow)).ToString(),
            challenge.CreatorId == currentUserId, members);
    }

    public static IQueryable<FitnessChallenge> IncludeAll(IApplicationDbContext db) =>
        db.FitnessChallenges.Include(x => x.Members).ThenInclude(x => x.User);
}

public sealed class CreateFitnessChallengeHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<CreateFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(CreateFitnessChallengeCommand request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var title = request.Title.Trim();
        if (title.Length is < 3 or > 80) throw new InvalidOperationException("Title must contain 3 to 80 characters.");
        if (request.TargetSessionsPerWeek is < 1 or > 7) throw new InvalidOperationException("Weekly target must be between 1 and 7.");
        if (request.StartsOn.DayOfWeek != DayOfWeek.Monday || request.StartsOn < today || request.StartsOn > today.AddDays(28))
            throw new InvalidOperationException("Challenge must start on a Monday within the next 28 days.");

        var subscription = await db.UserSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        var isPro = subscription is { Tier: not PlanTier.Free } && subscription.IsActive;
        var maxWeeks = isPro ? 12 : 4;
        var maxMembers = isPro ? 25 : 10;
        var maxOwned = isPro ? 5 : 1;
        if (request.DurationWeeks is < 1 || request.DurationWeeks > maxWeeks)
            throw new InvalidOperationException($"Your plan supports challenges up to {maxWeeks} weeks.");

        var ownedCount = await db.FitnessChallenges.AsNoTracking().CountAsync(x => x.CreatorId == currentUser.UserId &&
            x.CancelledAt == null && x.EndsOn >= today, ct);
        if (ownedCount >= maxOwned) throw new InvalidOperationException($"Your plan supports {maxOwned} upcoming or active challenge(s).");

        var inviteeIds = request.InviteeUserIds.Where(x => x != currentUser.UserId).Distinct().ToList();
        if (inviteeIds.Count == 0) throw new InvalidOperationException("Invite at least one friend or active client.");
        if (inviteeIds.Count + 1 > maxMembers) throw new InvalidOperationException($"Your plan supports up to {maxMembers} members.");

        var blockedIds = await db.UserBlocks.AsNoTracking().Where(x => x.BlockerId == currentUser.UserId || x.BlockedId == currentUser.UserId)
            .Select(x => x.BlockerId == currentUser.UserId ? x.BlockedId : x.BlockerId).ToListAsync(ct);
        var friendIds = await db.Friendships.AsNoTracking().Where(x => x.Status == FriendshipStatus.Accepted &&
            (x.UserAId == currentUser.UserId || x.UserBId == currentUser.UserId))
            .Select(x => x.UserAId == currentUser.UserId ? x.UserBId : x.UserAId).ToListAsync(ct);
        var clientIds = await db.CoachClientRelationships.AsNoTracking().Where(x => x.CoachId == currentUser.UserId && x.Status == RelationshipStatus.Active)
            .Select(x => x.ClientId).ToListAsync(ct);
        if (inviteeIds.Any(x => blockedIds.Contains(x) || (!friendIds.Contains(x) && !clientIds.Contains(x))))
            throw new InvalidOperationException("Invitees must be accepted friends or active clients.");

        var challenge = new FitnessChallenge
        {
            CreatorId = currentUser.UserId,
            Title = title,
            TargetSessionsPerWeek = request.TargetSessionsPerWeek,
            StartsOn = request.StartsOn,
            EndsOn = request.StartsOn.AddDays(request.DurationWeeks * 7 - 1),
            Members = [new FitnessChallengeMember { UserId = currentUser.UserId, Status = FitnessChallengeMemberStatus.Accepted, RespondedAt = DateTime.UtcNow },
                .. inviteeIds.Select(id => new FitnessChallengeMember { UserId = id })]
        };
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        foreach (var id in inviteeIds)
            await notifications.NotifyAsync(id, "FitnessChallengeInvite", $"You were invited to {title}.", challenge.Id, "FitnessChallenge", ct);

        var loaded = await FitnessChallengeRules.IncludeAll(db).AsNoTracking().FirstAsync(x => x.Id == challenge.Id, ct);
        return await FitnessChallengeRules.MapAsync(db, loaded, currentUser.UserId, ct);
    }
}

public sealed class GetFitnessChallengesHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetFitnessChallengesQuery, IReadOnlyList<FitnessChallengeResponse>>
{
    public async ValueTask<IReadOnlyList<FitnessChallengeResponse>> Handle(GetFitnessChallengesQuery request, CancellationToken ct)
    {
        var challenges = await FitnessChallengeRules.IncludeAll(db).AsNoTracking()
            .Where(x => x.Members.Any(m => m.UserId == currentUser.UserId &&
                m.Status != FitnessChallengeMemberStatus.Declined && m.Status != FitnessChallengeMemberStatus.Left))
            .OrderByDescending(x => x.StartsOn).ToListAsync(ct);
        var result = new List<FitnessChallengeResponse>();
        foreach (var challenge in challenges)
        {
            var dto = await FitnessChallengeRules.MapAsync(db, challenge, currentUser.UserId, ct);
            if (string.IsNullOrWhiteSpace(request.Status) || dto.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase)) result.Add(dto);
        }
        return result;
    }
}

public sealed class GetFitnessChallengeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetFitnessChallengeQuery, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(GetFitnessChallengeQuery request, CancellationToken ct)
    {
        var challenge = await FitnessChallengeRules.IncludeAll(db).AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (!challenge.Members.Any(x => x.UserId == currentUser.UserId && x.Status is not FitnessChallengeMemberStatus.Declined and not FitnessChallengeMemberStatus.Left))
            throw new UnauthorizedAccessException();
        return await FitnessChallengeRules.MapAsync(db, challenge, currentUser.UserId, ct);
    }
}

public sealed class GetChallengeInviteesHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetChallengeInviteesQuery, IReadOnlyList<ChallengeInviteeResponse>>
{
    public async ValueTask<IReadOnlyList<ChallengeInviteeResponse>> Handle(GetChallengeInviteesQuery request, CancellationToken ct)
    {
        var blockedIds = await db.UserBlocks.AsNoTracking().Where(x => x.BlockerId == currentUser.UserId || x.BlockedId == currentUser.UserId)
            .Select(x => x.BlockerId == currentUser.UserId ? x.BlockedId : x.BlockerId).ToListAsync(ct);
        var friendIds = await db.Friendships.AsNoTracking().Where(x => x.Status == FriendshipStatus.Accepted &&
                (x.UserAId == currentUser.UserId || x.UserBId == currentUser.UserId))
            .Select(x => x.UserAId == currentUser.UserId ? x.UserBId : x.UserAId).ToListAsync(ct);
        var clientIds = await db.CoachClientRelationships.AsNoTracking().Where(x => x.CoachId == currentUser.UserId && x.Status == RelationshipStatus.Active)
            .Select(x => x.ClientId).ToListAsync(ct);
        var ids = friendIds.Concat(clientIds).Distinct().Where(x => !blockedIds.Contains(x)).ToList();
        var users = await db.ApplicationUsers.AsNoTracking().Where(x => ids.Contains(x.Id)).OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync(ct);
        return users.Select(x => new ChallengeInviteeResponse(x.Id, $"{x.FirstName} {x.LastName}".Trim(), x.AvatarUrl,
            clientIds.Contains(x.Id) ? "Client" : "Friend")).ToList();
    }
}

public sealed class AcceptFitnessChallengeHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<AcceptFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(AcceptFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await FitnessChallengeRules.IncludeAll(db).FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (DateOnly.FromDateTime(DateTime.UtcNow) >= challenge.StartsOn) throw new InvalidOperationException("This invitation has expired.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId && x.Status == FitnessChallengeMemberStatus.Invited)
            ?? throw new InvalidOperationException("Challenge invitation not found.");
        member.Status = FitnessChallengeMemberStatus.Accepted; member.RespondedAt = DateTime.UtcNow; member.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifications.NotifyAsync(challenge.CreatorId, "FitnessChallengeAccepted", "A member accepted your challenge invitation.", challenge.Id, "FitnessChallenge", ct);
        return await FitnessChallengeRules.MapAsync(db, challenge, currentUser.UserId, ct);
    }
}

public sealed class DeclineFitnessChallengeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeclineFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(DeclineFitnessChallengeCommand request, CancellationToken ct)
    {
        var member = await db.FitnessChallengeMembers.FirstOrDefaultAsync(x => x.ChallengeId == request.ChallengeId && x.UserId == currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Challenge invitation not found.");
        if (member.Status != FitnessChallengeMemberStatus.Invited) throw new InvalidOperationException("Only pending invitations can be declined.");
        member.Status = FitnessChallengeMemberStatus.Declined; member.RespondedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class LeaveFitnessChallengeHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<LeaveFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(LeaveFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId == currentUser.UserId) throw new InvalidOperationException("The creator must cancel the challenge instead.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId && x.Status == FitnessChallengeMemberStatus.Accepted)
            ?? throw new InvalidOperationException("Active membership not found.");
        member.Status = FitnessChallengeMemberStatus.Left; member.RespondedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Unit.Value;
    }
}

public sealed class CancelFitnessChallengeHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<CancelFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(CancelFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (challenge.CancelledAt.HasValue) return Unit.Value;
        challenge.CancelledAt = DateTime.UtcNow; challenge.Status = FitnessChallengeStatus.Cancelled; await db.SaveChangesAsync(ct);
        foreach (var member in challenge.Members.Where(x => x.UserId != currentUser.UserId && x.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited))
            await notifications.NotifyAsync(member.UserId, "FitnessChallengeCancelled", $"{challenge.Title} was cancelled.", challenge.Id, "FitnessChallenge", ct);
        return Unit.Value;
    }
}
