using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Xenoh.Application.Features.Notifications.Commands.MarkNotificationRead;
using Xenoh.Application.Features.Notifications.Queries.GetMyNotifications;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Application.Tests.Features.Notifications;

public sealed class NotificationHandlerTests : HandlerTestBase
{
    private MarkNotificationReadHandler CreateMarkReadHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser());

    private GetMyNotificationsHandler CreateGetHandler(ApplicationDbContext ctx) =>
        new(ctx, CurrentUser());

    // ── MarkNotificationRead ────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_WhenOwnNotification_SetsIsReadTrue()
    {
        var id = await SeedNotificationAsync(UserId, isRead: false);

        await using var ctx = CreateContext();
        await CreateMarkReadHandler(ctx).Handle(new MarkNotificationReadCommand(id), CancellationToken.None);

        await using var verify = CreateContext();
        var n = await verify.Notifications.FindAsync(id);
        n!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkRead_WhenOtherUsersNotification_Throws()
    {
        var id = await SeedNotificationAsync(Guid.NewGuid(), isRead: false);

        await using var ctx = CreateContext();
        var act = () => CreateMarkReadHandler(ctx).Handle(new MarkNotificationReadCommand(id), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification not found.");
    }

    [Fact]
    public async Task MarkRead_WhenNotificationNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateMarkReadHandler(ctx).Handle(new MarkNotificationReadCommand(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification not found.");
    }

    // ── MarkAllNotificationsRead ────────────────────────────────────────────

    [Fact(Skip = "MarkAllNotificationsReadHandler uses ExecuteUpdateAsync which is not supported by EF InMemory")]
    public async Task MarkAllRead_UpdatesAllUnreadForCurrentUser() { }

    // ── GetMyNotifications ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMyNotifications_ReturnsOnlyCurrentUsersNotifications()
    {
        await SeedNotificationAsync(UserId, isRead: false);
        await SeedNotificationAsync(UserId, isRead: true);
        await SeedNotificationAsync(Guid.NewGuid(), isRead: false); // different user

        await using var ctx = CreateContext();
        var result = await CreateGetHandler(ctx).Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMyNotifications_WhenNoNotifications_ReturnsEmpty()
    {
        await using var ctx = CreateContext();
        var result = await CreateGetHandler(ctx).Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyNotifications_IncludesReadAndUnread()
    {
        var unreadId = await SeedNotificationAsync(UserId, isRead: false);
        var readId   = await SeedNotificationAsync(UserId, isRead: true);

        await using var ctx = CreateContext();
        var result = await CreateGetHandler(ctx).Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Id == unreadId && !n.IsRead);
        result.Should().Contain(n => n.Id == readId   &&  n.IsRead);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Guid> SeedNotificationAsync(Guid recipientId, bool isRead)
    {
        await using var ctx = CreateContext();
        var notification = new Notification
        {
            RecipientId = recipientId,
            Type = "test",
            Message = "Test notification",
            IsRead = isRead
        };
        ctx.Notifications.Add(notification);
        await ctx.SaveChangesAsync();
        return notification.Id;
    }
}
