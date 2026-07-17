using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Community.Queries.GetCommunityUserProfile;
using Xenoh.Application.Features.Community.Queries.SearchCommunityUsers;
using Xenoh.Application.Features.Community;
using Xenoh.Application.Features.TrainingDayShares.Queries.GetUserTrainingDayShares;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/community")]
[Authorize]
public sealed class CommunityController(IMediator mediator) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? query,
        [FromQuery] string? discipline = null,
        [FromQuery] string? direction = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new SearchCommunityUsersQuery(query, discipline, direction, page, pageSize), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct) =>
        Ok(await mediator.Send(new GetCommunitySettingsQuery(), ct));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateCommunitySettingsCommand command, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(command, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetCommunityUserProfileQuery(userId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("users/{userId:guid}/training-day-shares")]
    public async Task<IActionResult> GetUserShares(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetUserTrainingDaySharesQuery(userId, page, pageSize), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
