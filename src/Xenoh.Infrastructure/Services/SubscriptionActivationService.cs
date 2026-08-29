using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Infrastructure.Services;

public sealed class SubscriptionActivationService(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<SubscriptionActivationService> logger
) : ISubscriptionActivationService
{
    public async Task ActivateAsync(PaymentOrder order, CancellationToken ct = default)
    {
        UserSubscription? subscription = null;

        await ExecuteAtomicallyAsync(async () =>
        {
            subscription = await db.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == order.UserId, ct)
                ?? throw new InvalidOperationException($"Subscription not found for user {order.UserId}.");

            await ApplyActivationAsync(order, subscription, ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        await NotifySafelyAsync(order, subscription!, ct);
    }

    public async Task ActivateComplimentaryAsync(
        PaymentOrder order,
        LegalAcceptance legalAcceptance,
        CancellationToken ct = default)
    {
        UserSubscription? subscription = null;

        await ExecuteAtomicallyAsync(async () =>
        {
            subscription = await db.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == order.UserId, ct);

            if (subscription is null)
            {
                subscription = new UserSubscription { UserId = order.UserId, Tier = PlanTier.Free };
                db.UserSubscriptions.Add(subscription);
            }

            order.SubscriptionId = subscription.Id;
            legalAcceptance.PaymentOrderId = order.Id;
            db.PaymentOrders.Add(order);
            db.LegalAcceptances.Add(legalAcceptance);

            await ApplyActivationAsync(order, subscription, ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        await NotifySafelyAsync(order, subscription!, ct);
    }

    private async Task ApplyActivationAsync(
        PaymentOrder order,
        UserSubscription subscription,
        CancellationToken ct)
    {
        var userExists = await db.ApplicationUsers.AsNoTracking()
            .AnyAsync(user => user.Id == order.UserId, ct);
        if (!userExists)
            throw new InvalidOperationException($"Subscription user not found. UserId: {order.UserId}.");

        var now = DateTime.UtcNow;
        var isSameTierRenewal = subscription.Tier == order.RequestedTier
            && subscription.ExpiresAt.HasValue
            && subscription.ExpiresAt > now;
        var baseDate = isSameTierRenewal ? subscription.ExpiresAt!.Value : now;

        subscription.Tier = order.RequestedTier;
        subscription.ExpiresAt = baseDate.AddMonths(order.DurationMonths);
        subscription.ExpiryReminderSentAt = null;
        subscription.UpdatedAt = now;

        var coachRoleId = await db.Roles.AsNoTracking()
            .Where(role => role.NormalizedName == UserRole.Coach.ToUpperInvariant())
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Required Identity role '{UserRole.Coach}' was not found.");

        var coachRole = await db.UserRoles
            .FirstOrDefaultAsync(role => role.UserId == order.UserId && role.RoleId == coachRoleId, ct);

        if (order.RequestedTier == PlanTier.ProCoach)
        {
            if (coachRole is null)
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = order.UserId, RoleId = coachRoleId });
        }
        else if (coachRole is not null)
        {
            db.UserRoles.Remove(coachRole);
        }
    }

    private async Task ExecuteAtomicallyAsync(Func<Task> operation, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            await operation();
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await operation();
        await transaction.CommitAsync(ct);
    }

    private async Task NotifySafelyAsync(
        PaymentOrder order,
        UserSubscription subscription,
        CancellationToken ct)
    {
        try
        {
            await notificationService.NotifyAsync(
                order.UserId,
                "SubscriptionActivated",
                $"Đăng ký gói {order.RequestedTier} đã được kích hoạt thành công. Hết hạn vào {subscription.ExpiresAt:dd/MM/yyyy}.",
                subscription.Id,
                "Subscription",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Subscription activation committed but notification delivery failed for order {OrderId} and user {UserId}",
                order.Id,
                order.UserId);
        }
    }
}
