using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Community;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Friends.Queries.GetFriends;

public sealed class GetFriendsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendResponse>>
{
    public async ValueTask<IReadOnlyList<FriendResponse>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var blockedIds = await db.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync(cancellationToken);

        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserAId == userId || f.UserBId == userId))
            .OrderByDescending(f => f.RespondedAt ?? f.UpdatedAt)
            .ToListAsync(cancellationToken);

        var friendIds = friendships
            .Select(f => CommunityMapping.OtherUserId(f, userId))
            .Where(id => !blockedIds.Contains(id))
            .ToList();

        var users = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return friendships
            .Select(f => (Friendship: f, FriendId: CommunityMapping.OtherUserId(f, userId)))
            .Where(x => users.ContainsKey(x.FriendId))
            .Select(x =>
            {
                var user = users[x.FriendId];
                return new FriendResponse(
                    user.Id,
                    CommunityMapping.FullName(user),
                    null,
                    user.AvatarUrl,
                    user.Bio,
                    x.Friendship.RespondedAt ?? x.Friendship.UpdatedAt);
            })
            .ToList();
    }
}
