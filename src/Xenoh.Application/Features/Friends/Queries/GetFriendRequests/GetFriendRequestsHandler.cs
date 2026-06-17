using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Community;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Friends.Queries.GetFriendRequests;

public sealed class GetFriendRequestsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetFriendRequestsQuery, IReadOnlyList<FriendRequestResponse>>
{
    public async ValueTask<IReadOnlyList<FriendRequestResponse>> Handle(
        GetFriendRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var direction = request.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase)
            ? "outgoing"
            : "incoming";

        var requests = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Pending)
            .Where(f => direction == "incoming" ? f.AddresseeId == userId : f.RequesterId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var otherIds = requests
            .Select(f => direction == "incoming" ? f.RequesterId : f.AddresseeId)
            .ToList();

        var users = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => otherIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return requests
            .Where(f => users.ContainsKey(direction == "incoming" ? f.RequesterId : f.AddresseeId))
            .Select(f =>
            {
                var user = users[direction == "incoming" ? f.RequesterId : f.AddresseeId];
                return new FriendRequestResponse(
                    f.Id,
                    user.Id,
                    CommunityMapping.FullName(user),
                    user.Email ?? string.Empty,
                    user.AvatarUrl,
                    direction,
                    f.Status.ToString(),
                    f.CreatedAt,
                    f.RespondedAt);
            })
            .ToList();
    }
}
