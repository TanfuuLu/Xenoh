using Mediator;

namespace Xenoh.Application.Features.Friends.Commands.RejectFriendRequest;

public sealed record RejectFriendRequestCommand(Guid RequestId) : IRequest;
