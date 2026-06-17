using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Community;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Friends.Commands.AcceptFriendRequest;

public sealed class AcceptFriendRequestHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications
) : IRequestHandler<AcceptFriendRequestCommand, FriendRequestResponse>
{
    public async ValueTask<FriendRequestResponse> Handle(
        AcceptFriendRequestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("Friend request not found.");

        if (friendship.AddresseeId != userId)
            throw new UnauthorizedAccessException();
        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Friend request is not pending.");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var accepter = await db.ApplicationUsers.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        var requester = await db.ApplicationUsers.AsNoTracking().FirstAsync(u => u.Id == friendship.RequesterId, cancellationToken);

        await notifications.NotifyAsync(
            friendship.RequesterId,
            "FriendRequestAccepted",
            $"{CommunityMapping.FullName(accepter)} accepted your friend request.",
            friendship.Id,
            "Friendship",
            cancellationToken);

        return new FriendRequestResponse(
            friendship.Id,
            requester.Id,
            CommunityMapping.FullName(requester),
            requester.Email ?? string.Empty,
            requester.AvatarUrl,
            "incoming",
            friendship.Status.ToString(),
            friendship.CreatedAt,
            friendship.RespondedAt);
    }
}
