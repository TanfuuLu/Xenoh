using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.PlanComments.Commands.DeletePlanComment;

public sealed class DeletePlanCommentHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICommentRealtimeService commentRealtimeService
) : IRequestHandler<DeletePlanCommentCommand>
{
    public async ValueTask<Unit> Handle(
        DeletePlanCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var comment = await db.PlanComments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && c.PlanId == request.PlanId,
                cancellationToken)
            ?? throw new InvalidOperationException("Comment not found.");

        if (comment.AuthorId != userId)
            throw new InvalidOperationException("You can only delete your own comments.");

        var plan = await db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        db.PlanComments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);

        var realtimeRecipients = plan.CreatedByCoachId.HasValue
            ? new[] { plan.OwnerId, plan.CreatedByCoachId.Value }
            : new[] { plan.OwnerId };

        await commentRealtimeService.PlanCommentDeletedAsync(
            request.PlanId,
            request.CommentId,
            realtimeRecipients,
            cancellationToken);

        return Unit.Value;
    }
}
