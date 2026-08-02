using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

public sealed class CreateFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<CreateFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(CreateFitnessChallengeCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var limits = await FitnessChallengeRules.LimitsAsync(db, currentUser.UserId, ct);
        var input = FitnessChallengeRules.ValidateInput(request.Input, now, limits.MaxWeeks, limits.MaxMembers);
        var activeOwnedCount = await db.FitnessChallenges.AsNoTracking()
            .CountAsync(x => x.CreatorId == currentUser.UserId &&
                             x.CancelledAt == null && x.EndsAtUtc >= now, ct);
        if (activeOwnedCount >= limits.MaxOwned)
            throw new InvalidOperationException($"Your plan supports {limits.MaxOwned} upcoming or active challenge(s).");

        var inviteeIds = input.InviteeUserIds.Where(x => x != currentUser.UserId).Distinct().ToList();
        if (inviteeIds.Count + 1 > input.Capacity)
            throw new InvalidOperationException("Invitations exceed the selected participant capacity.");
        await FitnessChallengeRules.EnsureInviteesEligibleAsync(db, currentUser.UserId, inviteeIds, ct);

        var challenge = new FitnessChallenge
        {
            CreatorId = currentUser.UserId,
            Title = input.Title,
            Description = input.Description,
            MetricType = input.MetricType,
            AccessType = input.AccessType,
            TargetSessionsPerWeek = input.TargetSessionsPerWeek,
            SelectedLifts = input.SelectedLifts.ToList(),
            CheckInPrompt = input.CheckInPrompt,
            Capacity = input.Capacity,
            TimeZoneId = input.TimeZoneId,
            StartsAtUtc = input.StartsAtUtc,
            EndsAtUtc = input.EndsAtUtc,
            Members =
            [
                new FitnessChallengeMember
                {
                    UserId = currentUser.UserId,
                    Status = FitnessChallengeMemberStatus.Accepted,
                    RespondedAt = now
                },
                .. inviteeIds.Select(id => new FitnessChallengeMember { UserId = id })
            ]
        };
        db.FitnessChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        foreach (var id in inviteeIds)
            await notifications.NotifyAsync(
                id,
                "FitnessChallengeInvite",
                $"You were invited to {challenge.Title}.",
                challenge.Id,
                "FitnessChallenge",
                ct);
        return await LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }

    internal static async Task<FitnessChallengeResponse> LoadResponseAsync(
        IApplicationDbContext db,
        Guid challengeId,
        Guid currentUserId,
        CancellationToken ct)
    {
        var challenge = await FitnessChallengeRules.IncludeAll(db).AsNoTracking()
            .FirstAsync(x => x.Id == challengeId, ct);
        return (await FitnessChallengeMapping.MapManyAsync(db, [challenge], currentUserId, true, ct)).Single();
    }
}

public sealed class UpdateFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(UpdateFitnessChallengeCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenge = await FitnessChallengeRules.IncludeAll(db)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (now >= challenge.StartsAtUtc || challenge.CancelledAt.HasValue)
            throw new InvalidOperationException("Only upcoming challenges can be edited.");
        var limits = await FitnessChallengeRules.LimitsAsync(db, currentUser.UserId, ct);
        var input = FitnessChallengeRules.ValidateInput(request.Input, now, limits.MaxWeeks, limits.MaxMembers);
        var externalReservations = challenge.Members.Count(x => x.UserId != currentUser.UserId &&
            x.Status is FitnessChallengeMemberStatus.Invited or FitnessChallengeMemberStatus.Accepted);
        if (input.Capacity < externalReservations + 1)
            throw new InvalidOperationException("Capacity cannot be lower than reserved membership.");
        if (externalReservations > 0 &&
            (input.MetricType != challenge.MetricType ||
             !input.SelectedLifts.Order().SequenceEqual(challenge.SelectedLifts.Order())))
            throw new InvalidOperationException("Metric and lift selection lock after the first invitation or join.");

        challenge.Title = input.Title;
        challenge.Description = input.Description;
        challenge.MetricType = input.MetricType;
        challenge.AccessType = input.AccessType;
        challenge.TargetSessionsPerWeek = input.TargetSessionsPerWeek;
        challenge.SelectedLifts = input.SelectedLifts.ToList();
        challenge.CheckInPrompt = input.CheckInPrompt;
        challenge.Capacity = input.Capacity;
        challenge.TimeZoneId = input.TimeZoneId;
        challenge.StartsAtUtc = input.StartsAtUtc;
        challenge.EndsAtUtc = input.EndsAtUtc;
        challenge.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class GetFitnessChallengesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFitnessChallengesQuery, IReadOnlyList<FitnessChallengeResponse>>
{
    public async ValueTask<IReadOnlyList<FitnessChallengeResponse>> Handle(GetFitnessChallengesQuery request, CancellationToken ct)
    {
        var challenges = await FitnessChallengeRules.IncludeAll(db).AsNoTracking()
            .Where(x => x.Members.Any(m => m.UserId == currentUser.UserId &&
                m.Status != FitnessChallengeMemberStatus.Declined &&
                m.Status != FitnessChallengeMemberStatus.Left &&
                m.Status != FitnessChallengeMemberStatus.Removed))
            .OrderByDescending(x => x.StartsAtUtc)
            .ToListAsync(ct);
        var result = await FitnessChallengeMapping.MapManyAsync(db, challenges, currentUser.UserId, true, ct);
        return string.IsNullOrWhiteSpace(request.Status)
            ? result
            : result.Where(x => x.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

public sealed class GetDiscoverableFitnessChallengesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetDiscoverableFitnessChallengesQuery, IReadOnlyList<FitnessChallengeSummaryResponse>>
{
    public async ValueTask<IReadOnlyList<FitnessChallengeSummaryResponse>> Handle(
        GetDiscoverableFitnessChallengesQuery request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var blocked = await FitnessChallengeRules.BlockedIdsAsync(db, currentUser.UserId, ct);
        var connections = await FitnessChallengeRules.ConnectionIdsAsync(db, currentUser.UserId, ct);
        var challenges = await FitnessChallengeRules.IncludeAll(db).AsNoTracking()
            .Where(x => x.CancelledAt == null && x.StartsAtUtc > now &&
                        x.CreatorId != currentUser.UserId &&
                        !blocked.Contains(x.CreatorId) &&
                        (x.AccessType == FitnessChallengeAccessType.Community ||
                         (x.AccessType == FitnessChallengeAccessType.Connections &&
                          connections.Contains(x.CreatorId))) &&
                        !x.Members.Any(m => m.UserId == currentUser.UserId &&
                            (m.Status == FitnessChallengeMemberStatus.Accepted ||
                             m.Status == FitnessChallengeMemberStatus.Invited ||
                             m.Status == FitnessChallengeMemberStatus.Removed)))
            .OrderBy(x => x.StartsAtUtc)
            .Take(100)
            .ToListAsync(ct);
        return challenges.Select(x => FitnessChallengeMapping.Summary(x, currentUser.UserId, now)).ToList();
    }
}

public sealed class GetFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFitnessChallengeQuery, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(GetFitnessChallengeQuery request, CancellationToken ct)
    {
        var challenge = await FitnessChallengeRules.IncludeAll(db).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId);
        var isMember = member?.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited;
        if (!isMember)
        {
            if (FitnessChallengeRules.Status(challenge, DateTime.UtcNow) != FitnessChallengeStatus.Upcoming ||
                !await FitnessChallengeRules.CanDiscoverAsync(db, challenge, currentUser.UserId, ct))
                throw new UnauthorizedAccessException();
        }
        return (await FitnessChallengeMapping.MapManyAsync(db, [challenge], currentUser.UserId, false, ct)).Single();
    }
}

public sealed class GetChallengeInviteesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetChallengeInviteesQuery, IReadOnlyList<ChallengeInviteeResponse>>
{
    public async ValueTask<IReadOnlyList<ChallengeInviteeResponse>> Handle(GetChallengeInviteesQuery request, CancellationToken ct)
    {
        var blocked = await FitnessChallengeRules.BlockedIdsAsync(db, currentUser.UserId, ct);
        var friendIds = await db.Friendships.AsNoTracking()
            .Where(x => x.Status == FriendshipStatus.Accepted &&
                        (x.UserAId == currentUser.UserId || x.UserBId == currentUser.UserId))
            .Select(x => x.UserAId == currentUser.UserId ? x.UserBId : x.UserAId)
            .ToListAsync(ct);
        var relationships = await db.CoachClientRelationships.AsNoTracking()
            .Where(x => x.Status == RelationshipStatus.Active &&
                        (x.CoachId == currentUser.UserId || x.ClientId == currentUser.UserId))
            .Select(x => new
            {
                UserId = x.CoachId == currentUser.UserId ? x.ClientId : x.CoachId,
                Relationship = x.CoachId == currentUser.UserId ? "Client" : "Coach"
            })
            .ToListAsync(ct);
        var ids = friendIds.Concat(relationships.Select(x => x.UserId))
            .Distinct().Where(x => !blocked.Contains(x)).ToList();
        var users = await db.ApplicationUsers.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .ToListAsync(ct);
        return users.Select(x => new ChallengeInviteeResponse(
            x.Id,
            $"{x.FirstName} {x.LastName}".Trim(),
            x.AvatarUrl,
            relationships.FirstOrDefault(r => r.UserId == x.Id)?.Relationship ?? "Friend"))
            .ToList();
    }
}

public sealed class AcceptFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<AcceptFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(AcceptFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await FitnessChallengeRules.IncludeAll(db)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (DateTime.UtcNow >= challenge.StartsAtUtc)
            throw new InvalidOperationException("This invitation has expired.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId &&
                                                            x.Status == FitnessChallengeMemberStatus.Invited)
            ?? throw new InvalidOperationException("Challenge invitation not found.");
        member.Status = FitnessChallengeMemberStatus.Accepted;
        member.RespondedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifications.NotifyAsync(
            challenge.CreatorId,
            "FitnessChallengeAccepted",
            "A member accepted your challenge invitation.",
            challenge.Id,
            "FitnessChallenge",
            ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class DeclineFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<DeclineFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(DeclineFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId)
            ?? throw new InvalidOperationException("Challenge invitation not found.");
        if (member.Status != FitnessChallengeMemberStatus.Invited)
            throw new InvalidOperationException("Only pending invitations can be declined.");
        member.Status = FitnessChallengeMemberStatus.Declined;
        member.RespondedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class JoinFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<JoinFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(JoinFitnessChallengeCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenge = await FitnessChallengeRules.IncludeAll(db)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CancelledAt.HasValue || now >= challenge.StartsAtUtc)
            throw new InvalidOperationException("Enrollment is closed.");
        if (challenge.AccessType == FitnessChallengeAccessType.InviteOnly)
            throw new InvalidOperationException("This challenge is invite-only.");
        if (!await FitnessChallengeRules.CanDiscoverAsync(db, challenge, currentUser.UserId, ct))
            throw new UnauthorizedAccessException();
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId);
        if (member?.Status == FitnessChallengeMemberStatus.Removed)
            throw new InvalidOperationException("The creator removed you from this challenge.");
        if (member?.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited)
            throw new InvalidOperationException("You already have a place in this challenge.");
        if (FitnessChallengeRules.ReservedCount(challenge) >= challenge.Capacity)
            throw new InvalidOperationException("This challenge is full.");
        if (member is null)
        {
            member = new FitnessChallengeMember
            {
                ChallengeId = challenge.Id,
                UserId = currentUser.UserId,
                Status = FitnessChallengeMemberStatus.Accepted,
                RespondedAt = now
            };
            challenge.Members.Add(member);
        }
        else
        {
            member.Status = FitnessChallengeMemberStatus.Accepted;
            member.RespondedAt = now;
            member.UpdatedAt = now;
        }
        challenge.UpdatedAt = now;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The last place was just taken. Refresh and try another challenge.");
        }
        await notifications.NotifyAsync(
            challenge.CreatorId,
            "FitnessChallengeJoined",
            "A member joined your challenge.",
            challenge.Id,
            "FitnessChallenge",
            ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class InviteFitnessChallengeMembersHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<InviteFitnessChallengeMembersCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(InviteFitnessChallengeMembersCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenge = await FitnessChallengeRules.IncludeAll(db)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (challenge.CancelledAt.HasValue || now >= challenge.StartsAtUtc)
            throw new InvalidOperationException("Invitations are closed.");
        var ids = request.UserIds.Where(x => x != currentUser.UserId).Distinct().ToList();
        await FitnessChallengeRules.EnsureInviteesEligibleAsync(db, currentUser.UserId, ids, ct);
        var existing = challenge.Members.ToDictionary(x => x.UserId);
        var reserving = ids.Count(id => !existing.TryGetValue(id, out var m) ||
            m.Status is FitnessChallengeMemberStatus.Declined or FitnessChallengeMemberStatus.Left);
        if (FitnessChallengeRules.ReservedCount(challenge) + reserving > challenge.Capacity)
            throw new InvalidOperationException("Invitations exceed the remaining participant capacity.");
        var notified = new List<Guid>();
        foreach (var id in ids)
        {
            if (existing.TryGetValue(id, out var member))
            {
                if (member.Status == FitnessChallengeMemberStatus.Removed) continue;
                if (member.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited) continue;
                member.Status = FitnessChallengeMemberStatus.Invited;
                member.RespondedAt = null;
                member.UpdatedAt = now;
            }
            else
            {
                challenge.Members.Add(new FitnessChallengeMember { UserId = id });
            }
            notified.Add(id);
        }
        challenge.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        foreach (var id in notified)
            await notifications.NotifyAsync(
                id,
                "FitnessChallengeInvite",
                $"You were invited to {challenge.Title}.",
                challenge.Id,
                "FitnessChallenge",
                ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class RemoveFitnessChallengeMemberHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<RemoveFitnessChallengeMemberCommand>
{
    public async ValueTask<Unit> Handle(RemoveFitnessChallengeMemberCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (request.UserId == challenge.CreatorId) throw new InvalidOperationException("The creator cannot be removed.");
        if (DateTime.UtcNow >= challenge.StartsAtUtc)
            throw new InvalidOperationException("Membership locks when the challenge starts.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == request.UserId &&
            x.Status is FitnessChallengeMemberStatus.Invited or FitnessChallengeMemberStatus.Accepted)
            ?? throw new InvalidOperationException("Reserved member not found.");
        member.Status = FitnessChallengeMemberStatus.Removed;
        member.RespondedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifications.NotifyAsync(
            member.UserId,
            "FitnessChallengeRemoved",
            $"You were removed from {challenge.Title}.",
            challenge.Id,
            "FitnessChallenge",
            ct);
        return Unit.Value;
    }
}

public sealed class LeaveFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<LeaveFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(LeaveFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId == currentUser.UserId)
            throw new InvalidOperationException("The creator must cancel the challenge instead.");
        if (FitnessChallengeRules.Status(challenge, DateTime.UtcNow) is
            FitnessChallengeStatus.Completed or FitnessChallengeStatus.Cancelled)
            throw new InvalidOperationException("This challenge is closed.");
        var member = challenge.Members.FirstOrDefault(x => x.UserId == currentUser.UserId &&
                                                            x.Status == FitnessChallengeMemberStatus.Accepted)
            ?? throw new InvalidOperationException("Active membership not found.");
        member.Status = FitnessChallengeMemberStatus.Left;
        member.RespondedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class CheckInFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CheckInFitnessChallengeCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(CheckInFitnessChallengeCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var challenge = await FitnessChallengeRules.IncludeAll(db)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.MetricType != FitnessChallengeMetricType.CustomCheckIns)
            throw new InvalidOperationException("Only custom challenges support check-ins.");
        if (FitnessChallengeRules.Status(challenge, now) != FitnessChallengeStatus.Active)
            throw new InvalidOperationException("Check-ins are available only while the challenge is active.");
        if (!challenge.Members.Any(x => x.UserId == currentUser.UserId &&
                                        x.Status == FitnessChallengeMemberStatus.Accepted))
            throw new UnauthorizedAccessException();
        var localDate = FitnessChallengeRules.LocalDate(challenge, now);
        var range = FitnessChallengeRules.LocalDateRange(challenge);
        if (localDate < range.Start || localDate >= range.EndExclusive)
            throw new InvalidOperationException(
                "Check-ins are available only within the challenge local date range.");
        var note = request.Note?.Trim();
        if ((note?.Length ?? 0) > 500) throw new InvalidOperationException("Check-in note must contain at most 500 characters.");
        if (await db.FitnessChallengeCheckIns.AsNoTracking().AnyAsync(x =>
                x.ChallengeId == challenge.Id && x.UserId == currentUser.UserId && x.LocalDate == localDate, ct))
            throw new InvalidOperationException("You already checked in today.");
        db.FitnessChallengeCheckIns.Add(new FitnessChallengeCheckIn
        {
            ChallengeId = challenge.Id,
            UserId = currentUser.UserId,
            LocalDate = localDate,
            Note = note
        });
        await db.SaveChangesAsync(ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class UndoFitnessChallengeCheckInHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UndoFitnessChallengeCheckInCommand, FitnessChallengeResponse>
{
    public async ValueTask<FitnessChallengeResponse> Handle(UndoFitnessChallengeCheckInCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.MetricType != FitnessChallengeMetricType.CustomCheckIns ||
            FitnessChallengeRules.Status(challenge, DateTime.UtcNow) != FitnessChallengeStatus.Active)
            throw new InvalidOperationException("Today's check-in cannot be changed.");
        var localDate = FitnessChallengeRules.LocalDate(challenge, DateTime.UtcNow);
        var checkIn = await db.FitnessChallengeCheckIns.FirstOrDefaultAsync(x =>
            x.ChallengeId == challenge.Id && x.UserId == currentUser.UserId && x.LocalDate == localDate, ct)
            ?? throw new InvalidOperationException("No check-in exists for today.");
        db.FitnessChallengeCheckIns.Remove(checkIn);
        await db.SaveChangesAsync(ct);
        return await CreateFitnessChallengeHandler.LoadResponseAsync(db, challenge.Id, currentUser.UserId, ct);
    }
}

public sealed class CancelFitnessChallengeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications)
    : IRequestHandler<CancelFitnessChallengeCommand>
{
    public async ValueTask<Unit> Handle(CancelFitnessChallengeCommand request, CancellationToken ct)
    {
        var challenge = await db.FitnessChallenges.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct)
            ?? throw new InvalidOperationException("Challenge not found.");
        if (challenge.CreatorId != currentUser.UserId) throw new UnauthorizedAccessException();
        if (challenge.CancelledAt.HasValue) return Unit.Value;
        challenge.CancelledAt = DateTime.UtcNow;
        challenge.Status = FitnessChallengeStatus.Cancelled;
        challenge.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        foreach (var member in challenge.Members.Where(x => x.UserId != currentUser.UserId &&
            x.Status is FitnessChallengeMemberStatus.Accepted or FitnessChallengeMemberStatus.Invited))
            await notifications.NotifyAsync(
                member.UserId,
                "FitnessChallengeCancelled",
                $"{challenge.Title} was cancelled.",
                challenge.Id,
                "FitnessChallenge",
                ct);
        return Unit.Value;
    }
}
