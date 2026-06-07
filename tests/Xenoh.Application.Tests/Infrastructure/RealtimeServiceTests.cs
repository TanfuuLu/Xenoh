using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Xenoh.Application.Features.Chat.Dtos;
using Xenoh.Application.Features.PlanComments.Dtos;
using Xenoh.Application.Tests.Common;
using Xenoh.Infrastructure.Hubs;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Infrastructure;

public sealed class RealtimeServiceTests : HandlerTestBase
{
    [Fact]
    public async Task MessageSentAsync_SendsReceiveMessageToDistinctRecipientGroups()
    {
        var hub = new FakeHubContext();
        var service = new ChatRealtimeService(hub);
        var recipient = Guid.NewGuid();
        var message = new MessageResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Coach User",
            "Check your top set.",
            "User",
            false,
            DateTime.UtcNow);

        await service.MessageSentAsync(
            message.RelationshipId,
            message,
            [recipient, recipient],
            CancellationToken.None);

        hub.Clients.Proxy.Sent.Should().ContainSingle();
        var sent = hub.Clients.Proxy.Sent.Single();
        sent.Method.Should().Be("ReceiveMessage");
        sent.Groups.Should().Equal($"user-{recipient}");
        sent.Arguments.Should().ContainSingle();
    }

    [Fact]
    public async Task CommentRealtimeService_SendsPlanAndWeekEventsToRecipientGroups()
    {
        var hub = new FakeHubContext();
        var service = new CommentRealtimeService(hub);
        var recipient = Guid.NewGuid();
        var comment = new CommentResponse(
            Guid.NewGuid(),
            "Looks good.",
            Guid.NewGuid(),
            "Client User",
            DateTime.UtcNow);

        await service.PlanCommentAddedAsync(Guid.NewGuid(), comment, [recipient], CancellationToken.None);
        await service.PlanCommentDeletedAsync(Guid.NewGuid(), comment.Id, [recipient], CancellationToken.None);
        await service.WeekCommentAddedAsync(Guid.NewGuid(), comment, [recipient], CancellationToken.None);
        await service.WeekCommentDeletedAsync(Guid.NewGuid(), comment.Id, [recipient], CancellationToken.None);

        hub.Clients.Proxy.Sent.Select(s => s.Method).Should().Equal(
            "ReceivePlanCommentAdded",
            "ReceivePlanCommentDeleted",
            "ReceiveWeekCommentAdded",
            "ReceiveWeekCommentDeleted");

        var expectedGroups = new[] { $"user-{recipient}" };
        hub.Clients.Proxy.Sent.Should().OnlyContain(s => s.Groups.SequenceEqual(expectedGroups));
    }

    [Fact]
    public async Task NotifyAsync_PersistsNotificationAndSendsReceiveNotificationToRecipientGroup()
    {
        await using var db = CreateContext();
        var hub = new FakeHubContext();
        var service = new NotificationService(db, hub);
        var recipient = Guid.NewGuid();

        await service.NotifyAsync(
            recipient,
            "PlanComment",
            "New comment",
            Guid.NewGuid(),
            "Plan",
            CancellationToken.None);

        db.Notifications.Should().ContainSingle(n =>
            n.RecipientId == recipient &&
            n.Type == "PlanComment" &&
            n.Message == "New comment" &&
            !n.IsRead);

        hub.Clients.Proxy.Sent.Should().ContainSingle();
        var sent = hub.Clients.Proxy.Sent.Single();
        sent.Method.Should().Be("ReceiveNotification");
        sent.Groups.Should().Equal($"user-{recipient}");
    }

    private sealed class FakeHubContext : IHubContext<NotificationHub>
    {
        public FakeHubClients Clients { get; } = new();
        public IGroupManager Groups { get; } = new FakeGroupManager();

        IHubClients IHubContext<NotificationHub>.Clients => Clients;
    }

    private sealed class FakeHubClients : IHubClients
    {
        public RecordingClientProxy Proxy { get; } = new();

        public IClientProxy All => Proxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;

        public IClientProxy Client(string connectionId) => Proxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;

        public IClientProxy Group(string groupName)
        {
            Proxy.ActiveGroups = [groupName];
            return Proxy;
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            Proxy.ActiveGroups = [groupName];
            return Proxy;
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            Proxy.ActiveGroups = groupNames.ToArray();
            return Proxy;
        }

        public IClientProxy User(string userId) => Proxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        public string[] ActiveGroups { get; set; } = [];
        public List<SentInvocation> Sent { get; } = [];

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(new SentInvocation(method, ActiveGroups, args));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record SentInvocation(
        string Method,
        IReadOnlyList<string> Groups,
        object?[] Arguments);
}
