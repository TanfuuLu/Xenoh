using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.DeactivatePlan;

public sealed record DeactivatePlanCommand(Guid PlanId) : IRequest<PlanResponse>;
