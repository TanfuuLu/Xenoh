using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed record RequestAccountDeletionCommand : IRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }
}
