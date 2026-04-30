using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.WeekComments.Commands.DeleteWeekComment;

public sealed class DeleteWeekCommentHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICommentRealtimeService commentRealtimeService
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

        var week = await db.WeeklyWorkouts
            .AsNoTracking()
            .Include(w => w.Plan)
            .FirstOrDefaultAsync(w => w.Id == request.WeeklyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Week not found.");

        db.WeeklyWorkoutComments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);

        var plan = week.Plan;
        var realtimeRecipients = plan.CreatedByCoachId.HasValue
            ? new[] { plan.OwnerId, plan.CreatedByCoachId.Value }
            : new[] { plan.OwnerId };

        await commentRealtimeService.WeekCommentDeletedAsync(
            request.WeeklyWorkoutId,
            request.CommentId,
            realtimeRecipients,
            cancellationToken);

        return Unit.Value;
    }
}
