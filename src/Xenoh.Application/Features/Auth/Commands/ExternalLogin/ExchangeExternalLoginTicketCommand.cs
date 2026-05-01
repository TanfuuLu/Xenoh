using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Auth.Commands.Register;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed record ExchangeExternalLoginTicketCommand : IRequest<AuthResponse>
{
    [Required]
    public required string Ticket { get; init; }
}
