using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Auth.Commands.Register;

namespace Xenoh.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand : IRequest<AuthResponse>
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
