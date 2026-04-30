using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.WeekComments.Commands.AddWeekComment;

public sealed class AddWeekCommentHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ICommentRealtimeService commentRealtimeService
) : IRequestHandler<AddWeekCommentCommand, CommentResponse>
{
    public async ValueTask<CommentResponse> Handle(
        AddWeekCommentCommand request, CancellationToken cancellationToken)
    {
        var authorId = currentUser.UserId;

        var week = await db.WeeklyWorkouts
            .AsNoTracking()
            .Include(w => w.Plan)
            .FirstOrDefaultAsync(w => w.Id == request.WeeklyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Week not found.");

        var comment = new WeeklyWorkoutComment
        {
            WeeklyWorkoutId = request.WeeklyWorkoutId,
            AuthorId = authorId,
            Content = request.Content
        };

        db.WeeklyWorkoutComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        var author = await db.ApplicationUsers
            .AsNoTracking()
            .FirstAsync(u => u.Id == authorId, cancellationToken);

        var authorName = $"{author.FirstName} {author.LastName}".Trim();

        var plan = week.Plan;
        var recipientId = authorId == plan.OwnerId
            ? plan.CreatedByCoachId
            : (Guid?)plan.OwnerId;

        if (recipientId.HasValue)
        {
            await notificationService.NotifyAsync(
                recipientId.Value,
                "NewComment",
                $"{authorName} đã bình luận trên tuần {week.WeekNumber} — '{plan.Name}'.",
                week.Id,
                $"Week:{plan.Id}",
                cancellationToken);
        }

        var response = new CommentResponse(comment.Id, comment.Content, authorId, authorName, comment.CreatedAt);
        var realtimeRecipients = recipientId.HasValue
            ? new[] { authorId, recipientId.Value }
            : new[] { authorId };

        await commentRealtimeService.WeekCommentAddedAsync(
            week.Id,
            response,
            realtimeRecipients,
            cancellationToken);

        return response;
    }
}
