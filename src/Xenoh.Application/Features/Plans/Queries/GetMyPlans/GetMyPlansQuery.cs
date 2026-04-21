using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Queries.GetMyPlans;

public sealed record GetMyPlansQuery : IRequest<List<PlanResponse>>;
