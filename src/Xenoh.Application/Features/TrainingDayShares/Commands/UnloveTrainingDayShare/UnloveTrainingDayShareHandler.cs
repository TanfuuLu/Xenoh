using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.UnloveTrainingDayShare;

public sealed class UnloveTrainingDayShareHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UnloveTrainingDayShareCommand, TrainingDayShareResponse>
{
    public async ValueTask<TrainingDayShareResponse> Handle(
        UnloveTrainingDayShareCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var share = await db.TrainingDayShares
            .Include(s => s.User)
            .Include(s => s.Loves)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == request.ShareId, cancellationToken)
            ?? throw new InvalidOperationException("Training day share not found.");

        await EnsureCanViewShareAsync(share.UserId, userId, cancellationToken);

        var love = share.Loves.FirstOrDefault(l => l.UserId == userId);
        if (love is not null)
        {
            db.TrainingDayShareLoves.Remove(love);
            share.Loves.Remove(love);
            await db.SaveChangesAsync(cancellationToken);
        }

        return TrainingDayShareMapping.ToResponse(share, userId);
    }

    private async Task EnsureCanViewShareAsync(Guid ownerId, Guid viewerId, CancellationToken cancellationToken)
    {
        if (ownerId == viewerId)
            return;

        var blocked = await db.UserBlocks
            .AsNoTracking()
            .AnyAsync(b =>
                (b.BlockerId == viewerId && b.BlockedId == ownerId) ||
                (b.BlockerId == ownerId && b.BlockedId == viewerId),
                cancellationToken);
        if (blocked)
            throw new UnauthorizedAccessException();

        var friends = await db.Friendships
            .AsNoTracking()
            .AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.UserAId == viewerId && f.UserBId == ownerId) ||
                 (f.UserAId == ownerId && f.UserBId == viewerId)),
                cancellationToken);
        if (!friends)
            throw new UnauthorizedAccessException();
    }
}
