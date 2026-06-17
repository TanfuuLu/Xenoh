using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Friends.Commands.SendFriendRequest;

public sealed record SendFriendRequestCommand : IRequest<FriendRequestResponse>
{
    [Required]
    public required Guid TargetUserId { get; init; }
}
