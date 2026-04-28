using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<MarkNotificationReadCommand>
{
    public async ValueTask<Unit> Handle(
        MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var notification = await db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.RecipientId == userId,
                cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
