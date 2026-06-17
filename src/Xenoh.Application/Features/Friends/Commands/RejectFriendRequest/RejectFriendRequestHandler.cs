using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Friends.Commands.RejectFriendRequest;

public sealed class RejectFriendRequestHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<RejectFriendRequestCommand>
{
    public async ValueTask<Unit> Handle(RejectFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("Friend request not found.");

        if (friendship.AddresseeId != userId)
            throw new UnauthorizedAccessException();
        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Friend request is not pending.");

        friendship.Status = FriendshipStatus.Rejected;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
