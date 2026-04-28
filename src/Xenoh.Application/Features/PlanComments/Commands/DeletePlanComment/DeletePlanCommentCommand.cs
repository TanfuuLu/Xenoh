using Mediator;

namespace Xenoh.Application.Features.PlanComments.Commands.DeletePlanComment;

public sealed record DeletePlanCommentCommand(Guid PlanId, Guid CommentId) : IRequest;
