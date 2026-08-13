using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionDailyLog;
using Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionProfile;
using Xenoh.Application.Features.Nutrition.Food.Commands.CreateCustomFood;
using Xenoh.Application.Features.Nutrition.Food.Commands.CreateFoodLog;
using Xenoh.Application.Features.Nutrition.Food.Commands.DeleteFoodLog;
using Xenoh.Application.Features.Nutrition.Food.Queries.GetFoodLogsForDate;
using Xenoh.Application.Features.Nutrition.Food.Queries.ResolveFood;
using Xenoh.Application.Features.Nutrition.Food.Queries.SearchFood;
using Xenoh.Application.Features.Nutrition.MealPlans;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionDailyLog;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionHistory;
using Xenoh.Application.Features.Nutrition.Queries.GetNutritionSummary;

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
            return Ok(await mediator.Send(command, ct));
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
            return Ok(await mediator.Send(command with { Date = date }, ct));
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

    // Meal plan endpoints

    [HttpGet("meal-plans/weeks/{startDate}")]
    public async Task<IActionResult> GetMealPlanWeek(DateOnly startDate, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetMealPlanWeekQuery(startDate), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("meal-plans/ranges")]
    public async Task<IActionResult> ApplyMealPlanTemplate(
        [FromBody] ApplyMealPlanTemplateCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { UserId = null }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("meal-plans/{date}")]
    public async Task<IActionResult> GetMealPlan(DateOnly date, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetMealPlanForDateQuery(date), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("meal-plans/{date}")]
    public async Task<IActionResult> UpsertMealPlan(
        DateOnly date,
        [FromBody] UpsertMealPlanForDateCommand command,
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("meal-plans/items/{itemId:guid}/check")]
    public async Task<IActionResult> CheckMealPlanItem(Guid itemId, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new CheckMealPlanItemCommand(itemId), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("meal-plans/items/{itemId:guid}/uncheck")]
    public async Task<IActionResult> UncheckMealPlanItem(Guid itemId, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new UncheckMealPlanItemCommand(itemId), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("clients/{clientId:guid}/summary")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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

    [HttpGet("clients/{clientId:guid}/logs/{date}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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

    [HttpGet("clients/{clientId:guid}/history")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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

    [HttpGet("clients/{clientId:guid}/meal-plans/{date}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetClientMealPlan(Guid clientId, DateOnly date, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetMealPlanForDateQuery(date, clientId), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("clients/{clientId:guid}/meal-plans/weeks/{startDate}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetClientMealPlanWeek(
        Guid clientId,
        DateOnly startDate,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetMealPlanWeekQuery(startDate, clientId), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("clients/{clientId:guid}/meal-plans/ranges")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> ApplyClientMealPlanTemplate(
        Guid clientId,
        [FromBody] ApplyMealPlanTemplateCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { UserId = clientId }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("clients/{clientId:guid}/meal-plans/{date}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> UpsertClientMealPlan(
        Guid clientId,
        DateOnly date,
        [FromBody] UpsertMealPlanForDateCommand command,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { Date = date, UserId = clientId }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── Food item endpoints ────────────────────────────────────────────────

    [HttpGet("foods/search")]
    public async Task<IActionResult> SearchFoods([FromQuery] string q, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        return Ok(await mediator.Send(new SearchFoodQuery(q, lang), ct));
    }

    [HttpGet("foods/resolve")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
    public async Task<IActionResult> ResolveFood([FromQuery] string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            return Ok(await mediator.Send(new ResolveFoodQuery(name), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("foods")]
    public async Task<IActionResult> CreateCustomFood([FromBody] CreateCustomFoodCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Food log endpoints ─────────────────────────────────────────────────

    [HttpGet("logs/{date}/foods")]
    public async Task<IActionResult> GetFoodLogsForDate(DateOnly date, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetFoodLogsForDateQuery(date), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("logs/{date}/foods")]
    public async Task<IActionResult> CreateFoodLog(DateOnly date, [FromBody] CreateFoodLogCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { Date = date, UserId = null }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("logs/{date}/foods/{foodLogId:guid}")]
    public async Task<IActionResult> DeleteFoodLog(DateOnly date, Guid foodLogId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteFoodLogCommand(foodLogId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
