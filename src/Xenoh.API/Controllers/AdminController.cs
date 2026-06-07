using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Admin;
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
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] ReportStatus? status, [FromQuery] ReportReason? reason, CancellationToken ct)
    {
        var result = await mediator.Send(new GetReportsQuery(status, reason), ct);
        return Ok(result);
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportSummary(CancellationToken ct)
    {
        var result = await mediator.Send(new GetReportSummaryQuery(), ct);
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

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] PlanTier? tier,
        [FromQuery] bool? suspended,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminUsersQuery(search, role, tier, suspended), ct);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetAdminUserDetailQuery(userId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(
        [FromQuery] PlanType? planType,
        [FromQuery] Guid? ownerId,
        [FromQuery] Guid? coachId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminPlansQuery(planType, ownerId, coachId, from, to, isActive), ct);
        return Ok(result);
    }

    [HttpGet("plans/{planId:guid}/analytics")]
    public async Task<IActionResult> GetPlanAnalytics(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetAdminPlanAnalyticsQuery(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] PaymentStatus? status,
        [FromQuery] PlanTier? tier,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminPaymentsQuery(status, tier, from, to), ct);
        return Ok(result);
    }

    [HttpGet("payments/summary")]
    public async Task<IActionResult> GetPaymentSummary(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminPaymentsSummaryQuery(), ct);
        return Ok(result);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] PlanTier? tier, [FromQuery] bool? active, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminSubscriptionsQuery(tier, active), ct);
        return Ok(result);
    }

    [HttpGet("ai-usage/summary")]
    public async Task<IActionResult> GetAiUsageSummary([FromQuery] DateOnly? periodStart, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminAiUsageSummaryQuery(periodStart), ct);
        return Ok(result);
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
