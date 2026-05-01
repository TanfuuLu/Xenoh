using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand : IRequest
{
    [Required]
    public required string OldPassword { get; init; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; init; }
}
