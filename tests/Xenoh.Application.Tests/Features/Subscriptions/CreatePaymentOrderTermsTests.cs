using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Application.Features.Subscriptions.Commands.CreatePaymentOrder;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class CreatePaymentOrderTermsTests : HandlerTestBase
{
    [Fact]
    public async Task Handle_WithCurrentTermsAcceptance_PersistsAcceptanceForCreatedOrder()
    {
        await using var db = CreateContext();
        var handler = new CreatePaymentOrderHandler(
            new SubscriptionRepository(db),
            new PaymentOrderRepository(db),
            db,
            CurrentUser(),
            new FakeBankInfo(),
            new HealthyPreflight(),
            new FakeSubscriptionActivation(db));

        var response = await handler.Handle(new CreatePaymentOrderCommand
        {
            RequestedTier = PlanTier.ProCoach,
            DurationMonths = 3,
            AcceptedTerms = true,
            TermsVersion = SubscriptionContract.CurrentTermsVersion
        }, CancellationToken.None);

        response.Amount.Should().Be(597_000m);
        var acceptance = await db.LegalAcceptances.SingleAsync();
        acceptance.UserId.Should().Be(UserId);
        acceptance.PaymentOrderId.Should().Be(response.OrderId);
        acceptance.DocumentType.Should().Be(LegalDocumentType.TermsOfService);
        acceptance.DocumentVersion.Should().Be(SubscriptionContract.CurrentTermsVersion);
    }

    [Fact]
    public async Task Handle_WithStaleTerms_DoesNotCreatePaymentState()
    {
        await using var db = CreateContext();
        var handler = new CreatePaymentOrderHandler(
            new SubscriptionRepository(db),
            new PaymentOrderRepository(db),
            db,
            CurrentUser(),
            new FakeBankInfo(),
            new HealthyPreflight(),
            new FakeSubscriptionActivation(db));

        var act = () => handler.Handle(new CreatePaymentOrderCommand
        {
            RequestedTier = PlanTier.ProIndividual,
            DurationMonths = 1,
            AcceptedTerms = true,
            TermsVersion = "2026-06-16"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        db.PaymentOrders.Should().BeEmpty();
        db.UserSubscriptions.Should().BeEmpty();
        db.LegalAcceptances.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithFullDiscount_CompletesOrderAndActivatesSubscriptionImmediately()
    {
        await using var db = CreateContext();
        db.PromotionCodes.Add(new PromotionCode
        {
            Code = "FREE100",
            DiscountType = PromotionDiscountType.Percent,
            DiscountValue = 100m,
            AppliesToTier = PlanTier.ProIndividual,
            MaxRedemptionsPerUser = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new CreatePaymentOrderHandler(
            new SubscriptionRepository(db),
            new PaymentOrderRepository(db),
            db,
            CurrentUser(),
            new FakeBankInfo(),
            new UnexpectedPreflight(),
            new FakeSubscriptionActivation(db));

        var response = await handler.Handle(new CreatePaymentOrderCommand
        {
            RequestedTier = PlanTier.ProIndividual,
            DurationMonths = 1,
            PromotionCode = "FREE100",
            AcceptedTerms = true,
            TermsVersion = SubscriptionContract.CurrentTermsVersion
        }, CancellationToken.None);

        response.Amount.Should().Be(0m);
        response.DiscountAmount.Should().Be(response.OriginalAmount);
        response.PaymentRequired.Should().BeFalse();

        var order = await db.PaymentOrders.SingleAsync();
        order.Status.Should().Be(PaymentStatus.Completed);
        order.PaidAt.Should().NotBeNull();

        var subscription = await db.UserSubscriptions.SingleAsync();
        subscription.Tier.Should().Be(PlanTier.ProIndividual);
        subscription.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithFullDiscount_DoesNotDependOnRedisAvailability()
    {
        await using var db = CreateContext();
        db.PromotionCodes.Add(new PromotionCode
        {
            Code = "FREE100",
            DiscountType = PromotionDiscountType.Percent,
            DiscountValue = 100m,
            MaxRedemptionsPerUser = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new CreatePaymentOrderHandler(
            new SubscriptionRepository(db),
            new PaymentOrderRepository(db),
            db,
            CurrentUser(),
            new FakeBankInfo(),
            new UnexpectedPreflight(),
            new FakeSubscriptionActivation(db));

        var response = await handler.Handle(new CreatePaymentOrderCommand
        {
            RequestedTier = PlanTier.ProIndividual,
            DurationMonths = 1,
            PromotionCode = "FREE100",
            AcceptedTerms = true,
            TermsVersion = SubscriptionContract.CurrentTermsVersion
        }, CancellationToken.None);

        response.Amount.Should().Be(0m);
        response.PaymentRequired.Should().BeFalse();
        (await db.PaymentOrders.SingleAsync()).Status.Should().Be(PaymentStatus.Completed);
        (await db.UserSubscriptions.SingleAsync()).Tier.Should().Be(PlanTier.ProIndividual);
        await db.LegalAcceptances.SingleAsync();
    }

    private sealed class FakeBankInfo : ISePayBankInfo
    {
        public string BankAccountNumber => "123456789";
        public string BankAccountName => "XENOH";
        public string BankName => "MBBank";
    }

    private sealed class HealthyPreflight : IPaymentPreflightService
    {
        public Task<PaymentPreflightResult> CheckAsync(CancellationToken ct) =>
            Task.FromResult(PaymentPreflightResult.Ok());
    }

    private sealed class UnexpectedPreflight : IPaymentPreflightService
    {
        public Task<PaymentPreflightResult> CheckAsync(CancellationToken ct) =>
            throw new InvalidOperationException("Payment preflight must not run for a free order.");
    }

    private sealed class FakeSubscriptionActivation(ApplicationDbContext db) : ISubscriptionActivationService
    {
        public async Task ActivateAsync(PaymentOrder order, CancellationToken ct = default)
        {
            var subscription = await db.UserSubscriptions.SingleAsync(s => s.UserId == order.UserId, ct);
            subscription.Tier = order.RequestedTier;
            subscription.ExpiresAt = DateTime.UtcNow.AddMonths(order.DurationMonths);
            await db.SaveChangesAsync(ct);
        }

        public async Task ActivateComplimentaryAsync(
            PaymentOrder order,
            LegalAcceptance legalAcceptance,
            CancellationToken ct = default)
        {
            var subscription = new UserSubscription { UserId = order.UserId, Tier = order.RequestedTier };
            subscription.ExpiresAt = DateTime.UtcNow.AddMonths(order.DurationMonths);
            order.SubscriptionId = subscription.Id;
            db.UserSubscriptions.Add(subscription);
            db.PaymentOrders.Add(order);
            db.LegalAcceptances.Add(legalAcceptance);
            await db.SaveChangesAsync(ct);
        }
    }

}
