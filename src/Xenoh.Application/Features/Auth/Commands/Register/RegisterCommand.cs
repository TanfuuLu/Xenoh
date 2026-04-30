using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand : IRequest<AuthResponse>
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }

    [Required]
    public required string Role { get; init; }
}

public sealed record AuthResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    string? AvatarUrl,
    IList<string> Roles
);
