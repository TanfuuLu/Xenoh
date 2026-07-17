using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Community.Queries.SearchCommunityUsers;

public sealed class SearchCommunityUsersHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<SearchCommunityUsersQuery, PagedResponse<CommunityUserSummaryResponse>>
{
    public async ValueTask<PagedResponse<CommunityUserSummaryResponse>> Handle(
        SearchCommunityUsersQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var query = (request.Query ?? string.Empty).Trim().ToLowerInvariant();
        var page = PaginationDefaults.NormalizePageNumber(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        TrainingDiscipline? discipline = null;
        DevelopmentDirection? direction = null;
        if (!string.IsNullOrWhiteSpace(request.Discipline))
        {
            if (!Enum.TryParse<TrainingDiscipline>(request.Discipline, true, out var parsedDiscipline))
                throw new InvalidOperationException("Invalid training discipline.");
            discipline = parsedDiscipline;
        }
        if (!string.IsNullOrWhiteSpace(request.Direction))
        {
            if (!Enum.TryParse<DevelopmentDirection>(request.Direction, true, out var parsedDirection))
                throw new InvalidOperationException("Invalid development direction.");
            direction = parsedDirection;
        }

        if (query.Length < 2 && discipline is null && direction is null)
            return new PagedResponse<CommunityUserSummaryResponse>([], page, pageSize, 0, false);

        var blockedIds = await db.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync(cancellationToken);

        var baseQuery = db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id != userId && !blockedIds.Contains(u.Id))
            .Where(u => query.Length < 2 ||
                u.FirstName.ToLower().Contains(query) ||
                u.LastName.ToLower().Contains(query));

        if (discipline.HasValue)
            baseQuery = baseQuery.Where(u => u.TrainingDiscipline == discipline);
        if (direction.HasValue)
            baseQuery = baseQuery.Where(u => u.DevelopmentDirection == direction);

        var total = await baseQuery.CountAsync(cancellationToken);

        var users = await baseQuery
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UserAId == userId ? f.UserBId : f.UserAId) &&
                        (f.UserAId == userId || f.UserBId == userId) &&
                        f.Status != FriendshipStatus.Rejected)
            .ToListAsync(cancellationToken);

        var items = users
            .Select(u =>
            {
                var friendship = friendships.FirstOrDefault(f => f.UserAId == u.Id || f.UserBId == u.Id);
                return new CommunityUserSummaryResponse(
                    u.Id,
                    CommunityMapping.FullName(u),
                    null,
                    u.AvatarUrl,
                    u.Bio,
                    u.Gender?.ToString(),
                    friendship?.Status.ToString() ?? "None",
                    friendship?.Id,
                    friendship is null ? null : CommunityMapping.RequestDirection(friendship, userId));
            })
            .ToList();

        return new PagedResponse<CommunityUserSummaryResponse>(items, page, pageSize, total, page * pageSize < total);
    }
}
