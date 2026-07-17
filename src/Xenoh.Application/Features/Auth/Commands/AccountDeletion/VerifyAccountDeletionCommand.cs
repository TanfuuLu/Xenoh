using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed record VerifyAccountDeletionCommand : IRequest
{
    [Required]
    public required string Token { get; init; }
}
