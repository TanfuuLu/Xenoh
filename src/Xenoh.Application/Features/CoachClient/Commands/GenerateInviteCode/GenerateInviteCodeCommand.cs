using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;

public sealed record GenerateInviteCodeCommand : IRequest<CoachInviteCodeDto>
{
    public Xenoh.Application.Features.CoachClient.Agreements.AgreementTerms? Terms { get; init; }
    public bool Acknowledged { get; init; }
    [Required]
    public required DateOnly CoachingStartDate { get; init; }

    [Required]
    public required DateOnly CoachingEndDate { get; init; }
}

public sealed record CoachInviteCodeDto(
    Guid Id,
    string Code,
    DateOnly CoachingStartDate,
    DateOnly CoachingEndDate,
    bool IsUsed,
    Guid? UsedByClientId,
    DateTime? UsedAt,
    DateTime CreatedAt,
    Xenoh.Application.Features.CoachClient.Agreements.AgreementDto? Agreement = null,
    Guid? RelationshipId = null
);
