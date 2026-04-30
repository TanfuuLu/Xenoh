using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Common.Interfaces;

public interface ICommentRealtimeService
{
    Task PlanCommentAddedAsync(
        Guid planId,
        CommentResponse comment,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default);

    Task PlanCommentDeletedAsync(
        Guid planId,
        Guid commentId,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default);

    Task WeekCommentAddedAsync(
        Guid weekId,
        CommentResponse comment,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default);

    Task WeekCommentDeletedAsync(
        Guid weekId,
        Guid commentId,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default);
}
