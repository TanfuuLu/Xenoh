using Mediator;

namespace Xenoh.Application.Features.Friends.Queries.GetFriends;

public sealed record GetFriendsQuery : IRequest<IReadOnlyList<FriendResponse>>;
