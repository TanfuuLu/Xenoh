using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.WeekComments.Commands.DeleteWeekComment;

public sealed class DeleteWeekCommentHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteWeekCommentCommand>
{
    public async ValueTask<Unit> Handle(
        DeleteWeekCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var comment = await db.WeeklyWorkoutComments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && c.WeeklyWorkoutId == request.WeeklyWorkoutId,
                cancellationToken)
            ?? throw new InvalidOperationException("Comment not found.");

        if (comment.AuthorId != userId)
            throw new InvalidOperationException("You can only delete your own comments.");

        db.WeeklyWorkoutComments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
