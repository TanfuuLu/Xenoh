using Mediator;

namespace Xenoh.Application.Features.WeekComments.Commands.DeleteWeekComment;

public sealed record DeleteWeekCommentCommand(Guid WeeklyWorkoutId, Guid CommentId) : IRequest;
