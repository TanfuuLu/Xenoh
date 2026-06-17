using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Friends.Commands.RemoveFriend;

public sealed class RemoveFriendHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<RemoveFriendCommand>
{
    public async ValueTask<Unit> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserAId == userId && f.UserBId == request.UserId) ||
                (f.UserAId == request.UserId && f.UserBId == userId),
                cancellationToken)
            ?? throw new InvalidOperationException("Friendship not found.");

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
