using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed record VerifyForgotPasswordCodeCommand : IRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string Code { get; init; }
}
