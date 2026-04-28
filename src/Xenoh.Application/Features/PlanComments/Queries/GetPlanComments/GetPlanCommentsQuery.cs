using Mediator;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.PlanComments.Queries.GetPlanComments;

public sealed record GetPlanCommentsQuery(Guid PlanId) : IRequest<IReadOnlyList<CommentResponse>>;
