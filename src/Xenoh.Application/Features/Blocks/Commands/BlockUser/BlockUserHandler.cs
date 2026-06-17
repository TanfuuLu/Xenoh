using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Blocks.Commands.BlockUser;

public sealed class BlockUserHandler(
    IUserBlockRepository blockRepo,
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<BlockUserCommand>
{
    public async ValueTask<Unit> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        var blockerId = currentUser.UserId;

        if (blockerId == request.TargetUserId)
            throw new InvalidOperationException("Bạn không thể tự chặn chính mình.");

        var targetExists = await db.ApplicationUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);
        if (!targetExists)
            throw new InvalidOperationException("Người dùng không tồn tại.");

        var hasOpenRelationship = await db.CoachClientRelationships
            .AsNoTracking()
            .AnyAsync(r =>
                r.Status != RelationshipStatus.Ended &&
                ((r.ClientId == blockerId && r.CoachId == request.TargetUserId) ||
                 (r.CoachId == blockerId && r.ClientId == request.TargetUserId)),
                cancellationToken);
        if (hasOpenRelationship)
            throw new InvalidOperationException("Hãy kết thúc quan hệ huấn luyện trước khi chặn người dùng này.");

        var existing = await blockRepo.FindAsync(blockerId, request.TargetUserId, cancellationToken);
        if (existing is not null)
            return Unit.Value;

        var friendships = await db.Friendships
            .Where(f =>
                (f.UserAId == blockerId && f.UserBId == request.TargetUserId) ||
                (f.UserAId == request.TargetUserId && f.UserBId == blockerId))
            .ToListAsync(cancellationToken);
        if (friendships.Count > 0)
            db.Friendships.RemoveRange(friendships);

        await blockRepo.AddAsync(new UserBlock
        {
            BlockerId = blockerId,
            BlockedId = request.TargetUserId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        }, cancellationToken);

        await blockRepo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
