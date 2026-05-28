using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand : IRequest<RegisterResponse>
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }

    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }

    [Required]
    public required string Role { get; init; }

    [Required]
    public required Gender? Gender { get; init; }

    [Required]
    public required DateOnly? DateOfBirth { get; init; }

    public decimal? Height { get; init; }

    public decimal? Bodyweight { get; init; }
}

public sealed record RegisterResponse(Guid UserId, string Email);

public sealed record AuthResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    string? AvatarUrl,
    IList<string> Roles
);
