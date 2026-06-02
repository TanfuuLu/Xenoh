using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Notifications.Dtos;

namespace Xenoh.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async ValueTask<IReadOnlyList<NotificationResponse>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        return await db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(
                n.Id, n.Type, n.Message, n.IsRead,
                n.RelatedEntityId, n.RelatedEntityType, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
