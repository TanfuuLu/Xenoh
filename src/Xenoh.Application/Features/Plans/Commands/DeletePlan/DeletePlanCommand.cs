using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Plans.Commands.DeletePlan;

public sealed record DeletePlanCommand : IRequest
{
    [Required]
    public required Guid PlanId { get; init; }
}
