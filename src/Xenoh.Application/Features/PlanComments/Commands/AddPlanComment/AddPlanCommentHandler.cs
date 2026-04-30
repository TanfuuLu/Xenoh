using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.PlanComments.Commands.AddPlanComment;

public sealed class AddPlanCommentHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ICommentRealtimeService commentRealtimeService
) : IRequestHandler<AddPlanCommentCommand, CommentResponse>
{
    public async ValueTask<CommentResponse> Handle(
        AddPlanCommentCommand request, CancellationToken cancellationToken)
    {
        var authorId = currentUser.UserId;

        var plan = await db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var comment = new PlanComment
        {
            PlanId = request.PlanId,
            AuthorId = authorId,
            Content = request.Content
        };

        db.PlanComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        var author = await db.ApplicationUsers
            .AsNoTracking()
            .FirstAsync(u => u.Id == authorId, cancellationToken);

        var authorName = $"{author.FirstName} {author.LastName}".Trim();

        // Notify the other party
        var recipientId = authorId == plan.OwnerId
            ? plan.CreatedByCoachId   // client commenting → notify coach
            : (Guid?)plan.OwnerId;    // coach commenting → notify client

        if (recipientId.HasValue)
        {
            await notificationService.NotifyAsync(
                recipientId.Value,
                "NewComment",
                $"{authorName} đã bình luận trên kế hoạch '{plan.Name}'.",
                plan.Id,
                "Plan",
                cancellationToken);
        }

        var response = new CommentResponse(comment.Id, comment.Content, authorId, authorName, comment.CreatedAt);
        var realtimeRecipients = recipientId.HasValue
            ? new[] { authorId, recipientId.Value }
            : new[] { authorId };

        await commentRealtimeService.PlanCommentAddedAsync(
            plan.Id,
            response,
            realtimeRecipients,
            cancellationToken);

        return response;
    }
}
