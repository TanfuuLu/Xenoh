using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Auth.Commands.Register;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed record CompleteExternalRegistrationCommand : IRequest<AuthResponse>
{
    [Required]
    public required string Role { get; init; }
}
