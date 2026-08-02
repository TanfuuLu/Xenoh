using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Application.Features.Subscriptions.Commands.CreatePaymentOrder;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Enums;
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
            new HealthyPreflight());

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
            new HealthyPreflight());

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
}
