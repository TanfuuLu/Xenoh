using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Xenoh.Application.Features.Notifications.Commands.MarkNotificationRead;
using Xenoh.Application.Features.Notifications.Queries.GetMyNotifications;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyNotificationsQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new MarkNotificationReadCommand(notificationId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }
}
