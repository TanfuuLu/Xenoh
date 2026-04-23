using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.ActivatePlan;

public sealed record ActivatePlanCommand(Guid PlanId) : IRequest<PlanResponse>;
