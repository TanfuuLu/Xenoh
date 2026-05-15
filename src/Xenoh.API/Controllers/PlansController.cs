using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Plans.Commands.CreateAiStarterPlan;
using Xenoh.Application.Features.Plans.Commands.ActivatePlan;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.Plans.Commands.CreatePlanForUser;
using Xenoh.Application.Features.Plans.Commands.DeactivatePlan;
using Xenoh.Application.Features.Plans.Commands.DeletePlan;
using Xenoh.Application.Features.Plans.Commands.UpdatePlan;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetMyPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Application.Features.Plans.Queries.GetPlanById;
using Xenoh.Application.Features.Plans.Queries.ReviewPlanBalance;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize]
public sealed class PlansController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyPlans(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyPlansQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<IActionResult> GetById(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPlanByIdQuery(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { planId = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("starter-ai")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> CreateAiStarterPlan(
        [FromBody] CreateAiStarterPlanCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { planId = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Coach only] Lấy tất cả plans: cá nhân của coach + plans đã tạo cho clients.
    /// </summary>
    [HttpGet("coach-overview")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetCoachPlans(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoachPlansQuery(), ct);
        return Ok(result);
    }

    [HttpPost("for-user")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> CreatePlanForUser([FromBody] CreatePlanForUserCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { planId = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{planId:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { PlanId = planId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{planId:guid}")]
    public async Task<IActionResult> DeletePlan(Guid planId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeletePlanCommand { PlanId = planId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Activate a plan. Auto-deactivates any other active plan of the same owner.
    /// Only the plan owner can activate — coaches cannot activate on behalf of clients.
    /// Idempotent: activating an already-active plan returns 200 with current state.
    /// </summary>
    [HttpPatch("{planId:guid}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ActivatePlanCommand(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message == "Plan not found."
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate a plan. Only the plan owner can deactivate.
    /// Idempotent: deactivating an already-inactive plan returns 200 with current state.
    /// </summary>
    [HttpGet("{planId:guid}/analytics")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> GetPlanAnalytics(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPlanAnalyticsQuery(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{planId:guid}/balance-check")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> ReviewPlanBalance(
        Guid planId,
        [FromQuery] string? lang,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ReviewPlanBalanceQuery(planId, lang), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{planId:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePlan(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new DeactivatePlanCommand(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message == "Plan not found."
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }
}
