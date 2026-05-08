using Mediator;

namespace Xenoh.Application.Features.Blocks.Queries.GetMyBlocks;

public sealed record GetMyBlocksQuery : IRequest<List<BlockedUserResponse>>;

public sealed record BlockedUserResponse(
    Guid Id,
    Guid BlockedUserId,
    string FullName,
    string? AvatarUrl,
    string? Reason,
    DateTime CreatedAt
);
