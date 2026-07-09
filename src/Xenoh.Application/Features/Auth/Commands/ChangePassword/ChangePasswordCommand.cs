using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand : IRequest
{
    [Required]
    public required string OldPassword { get; init; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; init; }

    /// <summary>
    /// The caller's current access token, set server-side from the Authorization
    /// header (never trusted from the request body). Blacklisted on success so the
    /// current session dies immediately instead of at token expiry.
    /// </summary>
    public string? AccessToken { get; init; }
}
