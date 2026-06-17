using Mediator;

namespace Xenoh.Application.Features.Friends.Queries.GetFriendRequests;

public sealed record GetFriendRequestsQuery(string Direction) : IRequest<IReadOnlyList<FriendRequestResponse>>;
