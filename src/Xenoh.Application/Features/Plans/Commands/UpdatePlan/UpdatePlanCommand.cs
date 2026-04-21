using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.UpdatePlan;

public sealed record UpdatePlanCommand : IRequest<PlanResponse>
{
    public Guid PlanId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required]
    public required DateOnly StartDate { get; init; }

    [Required]
    public required DateOnly EndDate { get; init; }
}
