using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed record DeleteMyAccountCommand : IRequest
{
    [Required]
    public required string Password { get; init; }

    /// <summary>
    /// Set server-side from the Authorization header so the current access token
    /// can be revoked after account deletion.
    /// </summary>
    public string? AccessToken { get; init; }
}
