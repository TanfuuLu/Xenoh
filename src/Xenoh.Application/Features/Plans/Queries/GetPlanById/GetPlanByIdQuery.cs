using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetPlanById;

public sealed record GetPlanByIdQuery(Guid PlanId) : IRequest<PlanResponse>;
