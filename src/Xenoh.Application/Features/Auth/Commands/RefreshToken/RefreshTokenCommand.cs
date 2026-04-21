using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Auth.Commands.Register;

namespace Xenoh.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand : IRequest<AuthResponse>
{
    [Required]
    public required string RefreshToken { get; init; }
}
