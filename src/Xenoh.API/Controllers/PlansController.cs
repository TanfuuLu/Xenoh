using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Features.Plans.Commands.CreatePlanForUser;
using Xenoh.Application.Features.Plans.Commands.DeletePlan;
using Xenoh.Application.Features.Plans.Commands.UpdatePlan;
using Xenoh.Application.Features.Plans.Queries.GetCoachPlans;
using Xenoh.Application.Features.Plans.Queries.GetMyPlans;
using Xenoh.Application.Features.Plans.Queries.GetPlanById;
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

    /// <summary>
    /// [Coach only] Lấy tất cả plans: cá nhân của coach + plans đã tạo cho clients.
    /// </summary>
    [HttpGet("coach-overview")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetCoachPlans(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoachPlansQuery(), ct);
        return Ok(result);
    }

    [HttpPost("for-user")]
    [Authorize(Roles = UserRole.Coach)]
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
}
