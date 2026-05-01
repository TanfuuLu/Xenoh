using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.ExternalLogin;

public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl
) : IRequest<ExternalLoginTicketResponse>;

public sealed record ExternalLoginTicketResponse(string Ticket);
