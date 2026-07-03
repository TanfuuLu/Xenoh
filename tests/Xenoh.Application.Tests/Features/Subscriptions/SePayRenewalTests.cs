using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SePayRenewalTests
{
    [Fact]
    public async Task Handle_WhenSameTierRenewal_ExtendsFromCurrentExpiry()
    {
        var userId = Guid.NewGuid();
        var currentExpiry = DateTime.UtcNow.AddDays(10);
        var subscription = new UserSubscription
        {
            UserId = userId,
            Tier = PlanTier.ProCoach,
            ExpiresAt = currentExpiry
        };
        var order = CreateOrder(userId, subscription.Id, durationMonths: 1);
        var handler = CreateHandler(order, subscription);

        await handler.Handle(CreateWebhookCommand(order.TransferCode), CancellationToken.None);

        order.Status.Should().Be(PaymentStatus.Completed);
        subscription.Tier.Should().Be(order.RequestedTier);
        subscription.ExpiresAt.Should().BeCloseTo(currentExpiry.AddMonths(1), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenUpgradingTier_ResetsFromPaymentTime()
    {
        var userId = Guid.NewGuid();
        var beforePayment = DateTime.UtcNow;
        // Active Pro Individual with plenty of remaining time, upgrading to Pro Coach.
        var subscription = new UserSubscription
        {
            UserId = userId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(365)
        };
        var order = CreateOrder(userId, subscription.Id, durationMonths: 1);
        var handler = CreateHandler(order, subscription);

        await handler.Handle(CreateWebhookCommand(order.TransferCode), CancellationToken.None);

        order.Status.Should().Be(PaymentStatus.Completed);
        subscription.Tier.Should().Be(PlanTier.ProCoach);
        // Remaining 365 days do NOT carry over — term restarts from payment time.
        subscription.ExpiresAt.Should().BeAfter(beforePayment.AddMonths(1).AddSeconds(-2));
        subscription.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMonths(1).AddSeconds(2));
    }

    [Fact]
    public async Task Handle_WhenSubscriptionExpired_ExtendsFromPaymentTime()
    {
        var userId = Guid.NewGuid();
        var beforePayment = DateTime.UtcNow;
        var subscription = new UserSubscription
        {
            UserId = userId,
            Tier = PlanTier.ProIndividual,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        var order = CreateOrder(userId, subscription.Id, durationMonths: 1);
        var handler = CreateHandler(order, subscription);

        await handler.Handle(CreateWebhookCommand(order.TransferCode), CancellationToken.None);

        order.Status.Should().Be(PaymentStatus.Completed);
        subscription.Tier.Should().Be(order.RequestedTier);
        subscription.ExpiresAt.Should().BeAfter(beforePayment.AddMonths(1).AddSeconds(-2));
        subscription.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMonths(1).AddSeconds(2));
    }

    private static HandleSePayWebhookHandler CreateHandler(PaymentOrder order, UserSubscription subscription) =>
        new(
            new StubPaymentOrderRepository(order),
            new StubSubscriptionRepository(subscription),
            CreateUserManager(),
            new FakeNotificationService());

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

    private static UserManager<ApplicationUser> CreateUserManager() =>
        new(
            new NullUserStore(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

    private sealed class StubPaymentOrderRepository(PaymentOrder order) : IPaymentOrderRepository
    {
        public Task<PaymentOrder?> FindByTransferCodeAsync(string transferCode, CancellationToken ct = default) =>
            Task.FromResult(order.TransferCode == transferCode ? order : null);

        public Task<PaymentOrder?> FindBySePayTransactionIdAsync(string sePayTransactionId, CancellationToken ct = default) =>
            Task.FromResult<PaymentOrder?>(null);

        public Task<PaymentOrder?> FindPendingByUserAndTierAsync(Guid userId, PlanTier tier, CancellationToken ct = default) =>
            Task.FromResult<PaymentOrder?>(null);

        public Task<PaymentOrder?> FindLatestPendingByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<PaymentOrder?>(order);

        public Task AddAsync(PaymentOrder paymentOrder, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            Task.FromResult(1);
    }

    private sealed class StubSubscriptionRepository(UserSubscription subscription) : ISubscriptionRepository
    {
        public Task<UserSubscription?> FindByUserIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(subscription.UserId == userId ? subscription : null);

        public Task<UserSubscription?> GetByUserIdAsNoTrackingAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(subscription.UserId == userId ? subscription : null);

        public Task AddAsync(UserSubscription userSubscription, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            Task.FromResult(1);
    }

    private sealed class NullUserStore : IUserStore<ApplicationUser>
    {
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);

        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);
    }
}
