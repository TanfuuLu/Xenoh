using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.LoveTrainingDayShare;

public sealed class LoveTrainingDayShareHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService? notifications = null
) : IRequestHandler<LoveTrainingDayShareCommand, TrainingDayShareResponse>
{
    public async ValueTask<TrainingDayShareResponse> Handle(
        LoveTrainingDayShareCommand request,
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

        if (share.Loves.All(l => l.UserId != userId))
        {
            var love = new TrainingDayShareLove
            {
                TrainingDayShareId = share.Id,
                UserId = userId
            };
            db.TrainingDayShareLoves.Add(love);
            await db.SaveChangesAsync(cancellationToken);
            if (share.UserId != userId && notifications is not null)
                await notifications.NotifyAsync(share.UserId, "TrainingKudos",
            "A training partner supported your completed workout.", share.Id, "TrainingDayShare", cancellationToken);
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
