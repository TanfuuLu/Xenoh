using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Blocks.Queries.GetMyBlocks;

public sealed class GetMyBlocksHandler(
    IUserBlockRepository blockRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyBlocksQuery, List<BlockedUserResponse>>
{
    public ValueTask<List<BlockedUserResponse>> Handle(GetMyBlocksQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return new ValueTask<List<BlockedUserResponse>>(
            blockRepo.ListByBlockerAsync(userId, cancellationToken));
    }
}
