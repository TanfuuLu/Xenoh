using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SePayRenewalTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WhenSameTierRenewal_ExtendsFromCurrentExpiry()
    {
        var currentExpiry = DateTime.UtcNow.AddDays(10);
        await using var scenario = await CreateScenarioAsync(PlanTier.ProCoach, currentExpiry);

        await scenario.Handler.Handle(CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None);

        scenario.Order.Status.Should().Be(PaymentStatus.Completed);
        scenario.Subscription.Tier.Should().Be(scenario.Order.RequestedTier);
        scenario.Subscription.ExpiresAt.Should().BeCloseTo(currentExpiry.AddMonths(1), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenUpgradingTier_ResetsFromPaymentTime()
    {
        var beforePayment = DateTime.UtcNow;
        await using var scenario = await CreateScenarioAsync(
            PlanTier.ProIndividual,
            DateTime.UtcNow.AddDays(365));

        await scenario.Handler.Handle(CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None);

        scenario.Order.Status.Should().Be(PaymentStatus.Completed);
        scenario.Subscription.Tier.Should().Be(PlanTier.ProCoach);
        scenario.Subscription.ExpiresAt.Should().BeAfter(beforePayment.AddMonths(1).AddSeconds(-2));
        scenario.Subscription.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMonths(1).AddSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenSubscriptionExpired_ExtendsFromPaymentTime()
    {
        var beforePayment = DateTime.UtcNow;
        await using var scenario = await CreateScenarioAsync(
            PlanTier.ProIndividual,
            DateTime.UtcNow.AddDays(-1));

        await scenario.Handler.Handle(CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None);

        scenario.Order.Status.Should().Be(PaymentStatus.Completed);
        scenario.Subscription.Tier.Should().Be(scenario.Order.RequestedTier);
        scenario.Subscription.ExpiresAt.Should().BeAfter(beforePayment.AddMonths(1).AddSeconds(-2));
        scenario.Subscription.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMonths(1).AddSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenNotificationFails_DoesNotTurnCommittedActivationIntoFailure()
    {
        await using var scenario = await CreateScenarioAsync(
            PlanTier.Free,
            null,
            new ThrowingNotificationService());

        var result = await scenario.Handler.Handle(
            CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None);

        result.Success.Should().BeTrue();
        scenario.Order.Status.Should().Be(PaymentStatus.Completed);
        scenario.Subscription.Tier.Should().Be(PlanTier.ProCoach);
    }

    [Fact]
    public async Task Handle_WhenIdentityUserIsMissing_DoesNotReportActivationSuccess()
    {
        await using var scenario = await CreateScenarioAsync(PlanTier.Free, null, seedUser: false);

        var act = () => scenario.Handler.Handle(
            CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscription user not found.*");
    }

    [Fact]
    public async Task Handle_WhenProCoachActivates_AddsCoachRoleAtomically()
    {
        await using var scenario = await CreateScenarioAsync(PlanTier.Free, null);

        await scenario.Handler.Handle(CreateWebhookCommand(scenario.Order.TransferCode), CancellationToken.None);

        var hasCoachRole = await scenario.Db.UserRoles.AnyAsync(
            role => role.UserId == UserId && role.RoleId == scenario.CoachRoleId);
        hasCoachRole.Should().BeTrue();
    }

    private async Task<Scenario> CreateScenarioAsync(
        PlanTier currentTier,
        DateTime? currentExpiry,
        INotificationService? notificationService = null,
        bool seedUser = true)
    {
        var db = CreateContext();
        var coachRole = new IdentityRole<Guid>(UserRole.Coach)
        {
            Id = Guid.NewGuid(),
            NormalizedName = UserRole.Coach.ToUpperInvariant()
        };
        db.Roles.Add(coachRole);

        if (seedUser)
        {
            db.ApplicationUsers.Add(new ApplicationUser
            {
                Id = UserId,
                UserName = "renewal@example.com",
                NormalizedUserName = "RENEWAL@EXAMPLE.COM",
                Email = "renewal@example.com",
                NormalizedEmail = "RENEWAL@EXAMPLE.COM",
                FirstName = "Renewal",
                LastName = "User"
            });
        }

        var subscription = new UserSubscription
        {
            UserId = UserId,
            Tier = currentTier,
            ExpiresAt = currentExpiry
        };
        var order = CreateOrder(UserId, subscription.Id, durationMonths: 1);
        db.UserSubscriptions.Add(subscription);
        db.PaymentOrders.Add(order);
        await db.SaveChangesAsync();

        var activation = new SubscriptionActivationService(
            db,
            notificationService ?? new FakeNotificationService(),
            NullLogger<SubscriptionActivationService>.Instance);
        var handler = new HandleSePayWebhookHandler(new PaymentOrderRepository(db), activation);

        return new Scenario(db, handler, order, subscription, coachRole.Id);
    }

    private static PaymentOrder CreateOrder(Guid userId, Guid subscriptionId, int durationMonths) =>
        new()
        {
            UserId = userId,
            SubscriptionId = subscriptionId,
            RequestedTier = PlanTier.ProCoach,
            TransferCode = "XENOHABCDEF1212345678",
            Amount = 199_000m,
            DurationMonths = durationMonths,
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

    private static HandleSePayWebhookCommand CreateWebhookCommand(string transferCode) =>
        new(
            SePayId: Random.Shared.NextInt64(1, long.MaxValue),
            Gateway: "MBBank",
            TransactionDate: DateTime.UtcNow.ToString("O"),
            AccountNumber: "123456789",
            Code: transferCode,
            Content: transferCode,
            TransferType: "in",
            TransferAmount: 199_000m,
            ReferenceCode: Guid.NewGuid().ToString("N"),
            Description: transferCode);

    private sealed record Scenario(
        ApplicationDbContext Db,
        HandleSePayWebhookHandler Handler,
        PaymentOrder Order,
        UserSubscription Subscription,
        Guid CoachRoleId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class ThrowingNotificationService : INotificationService
    {
        public Task NotifyAsync(
            Guid recipientId,
            string type,
            string message,
            Guid? relatedEntityId = null,
            string? relatedEntityType = null,
            CancellationToken ct = default) =>
            throw new InvalidOperationException("Notification transport unavailable.");
    }
}
