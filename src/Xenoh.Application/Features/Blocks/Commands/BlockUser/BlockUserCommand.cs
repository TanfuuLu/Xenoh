using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Blocks.Commands.BlockUser;

public sealed record BlockUserCommand : IRequest
{
    [Required]
    public required Guid TargetUserId { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}
