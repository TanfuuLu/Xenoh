using Mediator;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.WeekComments.Queries.GetWeekComments;

public sealed record GetWeekCommentsQuery(Guid WeeklyWorkoutId) : IRequest<IReadOnlyList<CommentResponse>>;
