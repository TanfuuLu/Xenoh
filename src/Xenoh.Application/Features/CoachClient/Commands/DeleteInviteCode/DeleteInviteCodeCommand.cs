using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.DeleteInviteCode;

public sealed record DeleteInviteCodeCommand : IRequest
{
    [Required]
    public required Guid InviteCodeId { get; init; }
}
