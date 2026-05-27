using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Tests.Common;

public sealed class FakeCommentRealtimeService : ICommentRealtimeService
{
    public Task PlanCommentAddedAsync(Guid planId, CommentResponse comment, IReadOnlyCollection<Guid> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task PlanCommentDeletedAsync(Guid planId, Guid commentId, IReadOnlyCollection<Guid> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task WeekCommentAddedAsync(Guid weekId, CommentResponse comment, IReadOnlyCollection<Guid> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task WeekCommentDeletedAsync(Guid weekId, Guid commentId, IReadOnlyCollection<Guid> recipientIds, CancellationToken ct = default) => Task.CompletedTask;
}
