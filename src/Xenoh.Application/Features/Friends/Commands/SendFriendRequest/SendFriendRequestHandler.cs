using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Community;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Friends.Commands.SendFriendRequest;

public sealed class SendFriendRequestHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications
) : IRequestHandler<SendFriendRequestCommand, FriendRequestResponse>
{
    public async ValueTask<FriendRequestResponse> Handle(
        SendFriendRequestCommand request,
        CancellationToken cancellationToken)
    {
        var requesterId = currentUser.UserId;
        if (requesterId == request.TargetUserId)
            throw new InvalidOperationException("You cannot add yourself as a friend.");

        var target = await db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var requester = await db.ApplicationUsers
            .FirstAsync(u => u.Id == requesterId, cancellationToken);

        var isBlocked = await db.UserBlocks
            .AsNoTracking()
            .AnyAsync(b =>
                (b.BlockerId == requesterId && b.BlockedId == request.TargetUserId) ||
                (b.BlockerId == request.TargetUserId && b.BlockedId == requesterId),
                cancellationToken);
        if (isBlocked)
            throw new InvalidOperationException("You cannot send a friend request to this user.");

        var (userAId, userBId) = Friendship.NormalizePair(requesterId, request.TargetUserId);
        var existing = await db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == userAId && f.UserBId == userBId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                throw new InvalidOperationException("You are already friends.");

            if (existing.Status == FriendshipStatus.Pending)
            {
                if (existing.RequesterId == requesterId)
                    throw new InvalidOperationException("Friend request already sent.");

                existing.Status = FriendshipStatus.Accepted;
                existing.RespondedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                await notifications.NotifyAsync(
                    existing.RequesterId,
                    "FriendRequestAccepted",
                    $"{CommunityMapping.FullName(requester)} accepted your friend request.",
                    existing.Id,
                    "Friendship",
                    cancellationToken);

                return ToResponse(existing, target, requesterId, "outgoing");
            }

            existing.RequesterId = requesterId;
            existing.AddresseeId = request.TargetUserId;
            existing.Status = FriendshipStatus.Pending;
            existing.RespondedAt = null;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new Friendship
            {
                UserAId = userAId,
                UserBId = userBId,
                RequesterId = requesterId,
                AddresseeId = request.TargetUserId,
                Status = FriendshipStatus.Pending
            };
            db.Friendships.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken);

        await notifications.NotifyAsync(
            request.TargetUserId,
            "FriendRequestReceived",
            $"{CommunityMapping.FullName(requester)} sent you a friend request.",
            existing.Id,
            "Friendship",
            cancellationToken);

        return ToResponse(existing, target, requesterId, "outgoing");
    }

    private static FriendRequestResponse ToResponse(
        Friendship friendship,
        ApplicationUser user,
        Guid currentUserId,
        string direction) =>
        new(
            friendship.Id,
            user.Id,
            CommunityMapping.FullName(user),
            user.Email ?? string.Empty,
            user.AvatarUrl,
            direction,
            friendship.Status.ToString(),
            friendship.CreatedAt,
            friendship.RespondedAt);
}
