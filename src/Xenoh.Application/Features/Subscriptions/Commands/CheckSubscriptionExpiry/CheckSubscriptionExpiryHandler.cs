using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;

namespace Xenoh.Application.Features.Subscriptions.Commands.CheckSubscriptionExpiry;

public sealed class CheckSubscriptionExpiryHandler(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationService notificationService
) : IRequestHandler<CheckSubscriptionExpiryCommand, CheckSubscriptionExpiryResult>
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromDays(3);

    public async ValueTask<CheckSubscriptionExpiryResult> Handle(
        CheckSubscriptionExpiryCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var reminderCutoff = now.Add(ReminderWindow);

        var dueForReminder = await db.UserSubscriptions
            .Where(s => s.Tier != PlanTier.Free
                     && s.ExpiresAt != null
                     && s.ExpiresAt > now
                     && s.ExpiresAt <= reminderCutoff
                     && s.ExpiryReminderSentAt == null)
            .ToListAsync(cancellationToken);

        foreach (var s in dueForReminder)
            s.ExpiryReminderSentAt = now;

        var dueForExpiry = await db.UserSubscriptions
            .Where(s => s.Tier != PlanTier.Free && s.ExpiresAt != null && s.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        var expiredTiers = dueForExpiry.ToDictionary(s => s.Id, s => s.Tier);
        foreach (var s in dueForExpiry)
        {
            s.Tier = PlanTier.Free;
            s.ExpiryReminderSentAt = null;
            s.UpdatedAt = now;
        }

        if (dueForReminder.Count > 0 || dueForExpiry.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        foreach (var s in dueForReminder)
        {
            await notificationService.NotifyAsync(
                s.UserId,
                "SubscriptionExpiringSoon",
                $"Gói {s.Tier} của bạn sẽ hết hạn vào {s.ExpiresAt:dd/MM/yyyy}. Gia hạn ngay để không bị gián đoạn.",
                s.Id,
                "Subscription",
                cancellationToken);
        }

        foreach (var s in dueForExpiry)
        {
            var previousTier = expiredTiers[s.Id];

            var user = await userManager.FindByIdAsync(s.UserId.ToString());
            if (user is not null && previousTier is PlanTier.ProCoach or PlanTier.Organizer
                && await userManager.IsInRoleAsync(user, UserRole.Coach))
            {
                await userManager.RemoveFromRoleAsync(user, UserRole.Coach);
            }

            await notificationService.NotifyAsync(
                s.UserId,
                "SubscriptionExpired",
                $"Gói {previousTier} của bạn đã hết hạn và tài khoản đã chuyển về gói Free.",
                s.Id,
                "Subscription",
                cancellationToken);
        }

        return new CheckSubscriptionExpiryResult(dueForReminder.Count, dueForExpiry.Count);
    }
}
