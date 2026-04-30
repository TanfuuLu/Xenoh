using Microsoft.AspNetCore.SignalR;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;
using Xenoh.Infrastructure.Hubs;

namespace Xenoh.Infrastructure.Services;

public sealed class CommentRealtimeService(
    IHubContext<NotificationHub> hubContext
) : ICommentRealtimeService
{
    public Task PlanCommentAddedAsync(
        Guid planId,
        CommentResponse comment,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        return SendToUsersAsync(recipientIds, "ReceivePlanCommentAdded", new
        {
            PlanId = planId,
            Comment = comment
        }, ct);
    }

    public Task PlanCommentDeletedAsync(
        Guid planId,
        Guid commentId,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        return SendToUsersAsync(recipientIds, "ReceivePlanCommentDeleted", new
        {
            PlanId = planId,
            CommentId = commentId
        }, ct);
    }

    public Task WeekCommentAddedAsync(
        Guid weekId,
        CommentResponse comment,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        return SendToUsersAsync(recipientIds, "ReceiveWeekCommentAdded", new
        {
            WeekId = weekId,
            Comment = comment
        }, ct);
    }

    public Task WeekCommentDeletedAsync(
        Guid weekId,
        Guid commentId,
        IReadOnlyCollection<Guid> recipientIds,
        CancellationToken ct = default)
    {
        return SendToUsersAsync(recipientIds, "ReceiveWeekCommentDeleted", new
        {
            WeekId = weekId,
            CommentId = commentId
        }, ct);
    }

    private Task SendToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        string methodName,
        object payload,
        CancellationToken ct)
    {
        var groupNames = userIds
            .Distinct()
            .Select(userId => $"user-{userId}")
            .ToArray();

        if (groupNames.Length == 0)
            return Task.CompletedTask;

        return hubContext.Clients.Groups(groupNames).SendAsync(methodName, payload, ct);
    }
}
