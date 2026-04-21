using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Plans.Commands.CreatePlan;

public sealed record CreatePlanCommand : IRequest<PlanResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required]
    public required DateOnly StartDate { get; init; }

    [Required]
    public required DateOnly EndDate { get; init; }
}

public sealed record PlanResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string PlanType,
    Guid OwnerId,
    string OwnerName,
    Guid? CreatedByCoachId,
    string? CoachName,
    int TotalWeeks,
    int TotalDays,
    int CompletedDays,
    DateTime CreatedAt
);
