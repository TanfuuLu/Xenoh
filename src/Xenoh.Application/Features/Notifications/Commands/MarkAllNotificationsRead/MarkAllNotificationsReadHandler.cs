using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async ValueTask<Unit> Handle(
        MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        await db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

        return Unit.Value;
    }
}
