using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Queries.GetTrainingDayFeedPage;

public sealed record TrainingDayFeedPageResponse(IReadOnlyList<TrainingDayShareResponse> Items, string? NextCursor);
public sealed record GetTrainingDayFeedPageQuery(string Scope = "friends", string? Cursor = null, int PageSize = 20)
    : IRequest<TrainingDayFeedPageResponse>;

public sealed class GetTrainingDayFeedPageHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetTrainingDayFeedPageQuery, TrainingDayFeedPageResponse>
{
    public async ValueTask<TrainingDayFeedPageResponse> Handle(GetTrainingDayFeedPageQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);
        var blockedIds = await db.UserBlocks.AsNoTracking().Where(x => x.BlockerId == userId || x.BlockedId == userId)
            .Select(x => x.BlockerId == userId ? x.BlockedId : x.BlockerId).ToListAsync(ct);
        var visibleIds = request.Scope.Equals("mine", StringComparison.OrdinalIgnoreCase)
            ? [userId]
            : await db.Friendships.AsNoTracking().Where(x => x.Status == FriendshipStatus.Accepted &&
                    (x.UserAId == userId || x.UserBId == userId))
                .Select(x => x.UserAId == userId ? x.UserBId : x.UserAId).Where(x => !blockedIds.Contains(x)).ToListAsync(ct);
        if (!request.Scope.Equals("mine", StringComparison.OrdinalIgnoreCase)) visibleIds.Add(userId);

        var query = db.TrainingDayShares.AsNoTracking().Include(x => x.User).Include(x => x.Loves)
            .Where(x => visibleIds.Contains(x.UserId));
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (!DateTime.TryParse(request.Cursor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var before))
                throw new InvalidOperationException("Invalid feed cursor.");
            query = query.Where(x => x.CreatedAt < before);
        }

        var shares = await query.OrderByDescending(x => x.CreatedAt).Take(pageSize + 1).ToListAsync(ct);
        var hasMore = shares.Count > pageSize;
        if (hasMore) shares.RemoveAt(shares.Count - 1);
        var items = shares.Select(x => TrainingDayShareMapping.ToResponse(x, userId) with { Exercises = [] }).ToList();
        return new(items, hasMore ? shares[^1].CreatedAt.ToString("O", CultureInfo.InvariantCulture) : null);
    }
}
