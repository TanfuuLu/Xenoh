using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Queries.GetFriendTrainingDayFeed;

public sealed class GetFriendTrainingDayFeedHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetFriendTrainingDayFeedQuery, IReadOnlyList<TrainingDayShareResponse>>
{
    public async ValueTask<IReadOnlyList<TrainingDayShareResponse>> Handle(
        GetFriendTrainingDayFeedQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var page = PaginationDefaults.NormalizePageNumber(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        var blockedIds = await db.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync(cancellationToken);

        var friendIds = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserAId == userId || f.UserBId == userId))
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .Where(id => !blockedIds.Contains(id))
            .ToListAsync(cancellationToken);
        var visibleUserIds = friendIds.Append(userId).ToList();

        var shares = await db.TrainingDayShares
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Loves)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => visibleUserIds.Contains(s.UserId))
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return shares.Select(s => TrainingDayShareMapping.ToResponse(s, userId)).ToList();
    }
}
