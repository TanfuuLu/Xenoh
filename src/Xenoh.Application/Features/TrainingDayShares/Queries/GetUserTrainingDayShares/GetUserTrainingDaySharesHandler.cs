using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Queries.GetUserTrainingDayShares;

public sealed class GetUserTrainingDaySharesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetUserTrainingDaySharesQuery, IReadOnlyList<TrainingDayShareResponse>>
{
    public async ValueTask<IReadOnlyList<TrainingDayShareResponse>> Handle(
        GetUserTrainingDaySharesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var canView = userId == request.UserId || await db.Friendships
            .AsNoTracking()
            .AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.UserAId == userId && f.UserBId == request.UserId) ||
                 (f.UserAId == request.UserId && f.UserBId == userId)),
                cancellationToken);

        if (!canView)
            throw new UnauthorizedAccessException();

        var page = PaginationDefaults.NormalizePageNumber(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        var shares = await db.TrainingDayShares
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Loves)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return shares.Select(s => TrainingDayShareMapping.ToResponse(s, userId)).ToList();
    }
}
