using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.ForgotPassword;

public sealed record SendForgotPasswordCodeCommand : IRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
