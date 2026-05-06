using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionDailyLog;
using Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionProfile;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionDailyLog;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/nutrition")]
[Authorize]
public sealed class NutritionController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetNutritionSummaryQuery(), ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateNutritionProfileCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { UserId = null }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("logs/{date}")]
    public async Task<IActionResult> GetDailyLog(DateOnly date, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetNutritionDailyLogQuery(date), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("logs/{date}")]
    public async Task<IActionResult> UpdateDailyLog(
        DateOnly date,
        [FromBody] UpdateNutritionDailyLogCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { Date = date, UserId = null }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetNutritionHistoryQuery(from, to), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("clients/{clientId:guid}/summary")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetClientSummary(Guid clientId, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetNutritionSummaryQuery(clientId), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("clients/{clientId:guid}/profile")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> UpdateClientProfile(
        Guid clientId,
        [FromBody] UpdateNutritionProfileCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { UserId = clientId }, ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("clients/{clientId:guid}/logs/{date}")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetClientDailyLog(Guid clientId, DateOnly date, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetNutritionDailyLogQuery(date, clientId), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("clients/{clientId:guid}/logs/{date}")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> UpdateClientDailyLog(
        Guid clientId,
        DateOnly date,
        [FromBody] UpdateNutritionDailyLogCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { UserId = clientId, Date = date }, ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("clients/{clientId:guid}/history")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetClientHistory(
        Guid clientId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetNutritionHistoryQuery(from, to, clientId), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
