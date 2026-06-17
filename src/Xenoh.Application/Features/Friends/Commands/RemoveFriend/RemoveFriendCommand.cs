using Mediator;

namespace Xenoh.Application.Features.Friends.Commands.RemoveFriend;

public sealed record RemoveFriendCommand(Guid UserId) : IRequest;
