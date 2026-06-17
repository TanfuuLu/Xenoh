using Mediator;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.Community.Queries.SearchCommunityUsers;

public sealed record SearchCommunityUsersQuery(string? Query, int Page = 1, int PageSize = 20)
    : IRequest<PagedResponse<CommunityUserSummaryResponse>>;
