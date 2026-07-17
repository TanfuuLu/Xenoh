using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.TrainingDayShares.Commands.DeleteTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Commands.LoveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Commands.ShareTrainingDay;
using Xenoh.Application.Features.TrainingDayShares.Commands.UnloveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Queries.GetFriendTrainingDayFeed;
using Xenoh.Application.Features.TrainingDayShares.Queries.GetTrainingDayFeedPage;
using Xenoh.Application.Features.TrainingDayShares;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/training-day-shares")]
[Authorize]
public sealed class TrainingDaySharesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Share([FromBody] ShareTrainingDayCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetTrainingDayShareQuery(id), ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTrainingDayShareRequest request, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new UpdateTrainingDayShareCommand(id, request.Caption, request.IsReusable), ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/copy")]
    public async Task<IActionResult> Copy(Guid id, [FromBody] CopyTrainingShareRequest request, CancellationToken ct)
    {
        try { return Ok(new { exercisesCopied = await mediator.Send(new CopyReusableTrainingShareCommand(id, request.TargetDailyWorkoutId), ct) }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed(
        [FromQuery] string scope = "friends",
        [FromQuery] string? cursor = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (cursor is not null || !scope.Equals("friends", StringComparison.OrdinalIgnoreCase))
                return Ok(await mediator.Send(new GetTrainingDayFeedPageQuery(scope, cursor, pageSize), ct));
            // Compatibility for existing clients using page-based responses.
            if (Request.Query.ContainsKey("page"))
                return Ok(await mediator.Send(new GetFriendTrainingDayFeedQuery(page, pageSize), ct));
            return Ok(await mediator.Send(new GetTrainingDayFeedPageQuery(scope, null, pageSize), ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/love")]
    public async Task<IActionResult> Love(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new LoveTrainingDayShareCommand(id), ct);
            return Ok(result);
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

    [HttpDelete("{id:guid}/love")]
    public async Task<IActionResult> Unlove(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new UnloveTrainingDayShareCommand(id), ct);
            return Ok(result);
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

    [HttpPost("{id:guid}/kudos")]
    public Task<IActionResult> Kudos(Guid id, CancellationToken ct) => Love(id, ct);

    [HttpDelete("{id:guid}/kudos")]
    public Task<IActionResult> RemoveKudos(Guid id, CancellationToken ct) => Unlove(id, ct);

    [HttpPost("{id:guid}/reports")]
    public async Task<IActionResult> Report(Guid id, [FromBody] ReportTrainingDayShareRequest request, CancellationToken ct)
    {
        try
        {
            var reportId = await mediator.Send(new ReportTrainingDayShareCommand(id, request.Reason, request.Details), ct);
            return Ok(new { id = reportId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteTrainingDayShareCommand(id), ct);
            return NoContent();
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
}

public sealed record UpdateTrainingDayShareRequest(string? Caption, bool? IsReusable = null);
public sealed record CopyTrainingShareRequest(Guid TargetDailyWorkoutId);
public sealed record ReportTrainingDayShareRequest(Xenoh.Domain.Enums.ReportReason Reason, string Details);
