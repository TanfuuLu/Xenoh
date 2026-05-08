using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Blocks.Commands.UnblockUser;

public sealed record UnblockUserCommand : IRequest
{
    [Required]
    public required Guid TargetUserId { get; init; }
}
