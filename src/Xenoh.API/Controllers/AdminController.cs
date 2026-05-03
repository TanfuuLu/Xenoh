using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Reports.Commands.ReviewReport;
using Xenoh.Application.Features.Reports.Commands.SetUserSuspension;
using Xenoh.Application.Features.Reports.Queries.GetReports;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = UserRole.Admin)]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] ReportStatus? status, [FromQuery] ReportReason? reason, CancellationToken ct)
    {
        var result = await mediator.Send(new GetReportsQuery(status, reason), ct);
        return Ok(result);
    }

    [HttpPatch("reports/{reportId:guid}")]
    public async Task<IActionResult> ReviewReport(Guid reportId, [FromBody] ReviewReportCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { ReportId = reportId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new SetUserSuspensionCommand(userId, true), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId:guid}/unsuspend")]
    public async Task<IActionResult> UnsuspendUser(Guid userId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new SetUserSuspensionCommand(userId, false), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
