using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand : IRequest
{
    public string? AccessToken { get; init; }
}
