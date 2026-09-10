using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;

public sealed record ConnectByInviteCodeCommand : IRequest<CoachRelationshipResponse>
{
    public Guid AgreementId { get; init; }
    public bool Acknowledged { get; init; }
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public required string Code { get; init; }
}
