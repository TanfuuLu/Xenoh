using Mediator;
using Xenoh.Application.Features.Notifications.Dtos;

namespace Xenoh.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery : IRequest<IReadOnlyList<NotificationResponse>>;
