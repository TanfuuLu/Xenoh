using Mediator;

namespace Xenoh.Application.Features.Friends.Commands.AcceptFriendRequest;

public sealed record AcceptFriendRequestCommand(Guid RequestId) : IRequest<FriendRequestResponse>;
