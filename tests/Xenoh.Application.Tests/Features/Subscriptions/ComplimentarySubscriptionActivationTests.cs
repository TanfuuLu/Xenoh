using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Services;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class ComplimentarySubscriptionActivationTests : HandlerTestBase
{
    [Fact]
    public async Task ActivateComplimentaryAsync_PersistsAuditSubscriptionAndCoachRoleTogether()
    {
        await using var db = CreateContext();
        var coachRole = new IdentityRole<Guid>(UserRole.Coach)
        {
            Id = Guid.NewGuid(),
            NormalizedName = UserRole.Coach.ToUpperInvariant()
        };
        db.Roles.Add(coachRole);
        db.ApplicationUsers.Add(CreateUser());
        await db.SaveChangesAsync();

        var order = CreateOrder();
        var acceptance = CreateAcceptance(order.Id);
        var service = new SubscriptionActivationService(
            db,
            new FakeNotificationService(),
            NullLogger<SubscriptionActivationService>.Instance);

        await service.ActivateComplimentaryAsync(order, acceptance, CancellationToken.None);

        var persistedOrder = await db.PaymentOrders.SingleAsync();
        persistedOrder.Status.Should().Be(PaymentStatus.Completed);
        persistedOrder.Amount.Should().Be(0m);
        await db.LegalAcceptances.SingleAsync(a => a.PaymentOrderId == persistedOrder.Id);

        var subscription = await db.UserSubscriptions.SingleAsync(s => s.UserId == UserId);
        subscription.Tier.Should().Be(PlanTier.ProCoach);
        subscription.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await db.UserRoles.SingleAsync(role =>
            role.UserId == UserId && role.RoleId == coachRole.Id);
    }

    [Fact]
    public async Task ActivateComplimentaryAsync_WhenCoachRoleIsMissing_DoesNotPersistGrant()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(CreateUser());
        await db.SaveChangesAsync();

        var order = CreateOrder();
        var service = new SubscriptionActivationService(
            db,
            new FakeNotificationService(),
            NullLogger<SubscriptionActivationService>.Instance);

        var act = () => service.ActivateComplimentaryAsync(
            order,
            CreateAcceptance(order.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Required Identity role 'Coach' was not found.");
        db.PaymentOrders.Should().BeEmpty();
        db.LegalAcceptances.Should().BeEmpty();
        db.UserSubscriptions.Should().BeEmpty();
    }

    private ApplicationUser CreateUser() => new()
    {
        Id = UserId,
        UserName = "complimentary@example.com",
        NormalizedUserName = "COMPLIMENTARY@EXAMPLE.COM",
        Email = "complimentary@example.com",
        NormalizedEmail = "COMPLIMENTARY@EXAMPLE.COM",
        FirstName = "Complimentary",
        LastName = "User"
    };

    private PaymentOrder CreateOrder() => new()
    {
        UserId = UserId,
        RequestedTier = PlanTier.ProCoach,
        TransferCode = $"FREE{Guid.NewGuid():N}",
        Amount = 0m,
        DiscountAmount = 199_000m,
        DurationMonths = 1,
        Status = PaymentStatus.Completed,
        PaidAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };

    private LegalAcceptance CreateAcceptance(Guid orderId) => new()
    {
        UserId = UserId,
        DocumentType = LegalDocumentType.TermsOfService,
        DocumentVersion = SubscriptionContract.CurrentTermsVersion,
        AcceptedAt = DateTime.UtcNow,
        PaymentOrderId = orderId
    };
}
