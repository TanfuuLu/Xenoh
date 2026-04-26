using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/leaderboard")]
[Authorize]
public sealed class LeaderboardController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns top 50 ranked users.
    /// type: dots | squat | bench | deadlift
    /// gender: male | female (optional)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string type = "dots",
        [FromQuery] string? gender = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetLeaderboardQuery { Type = type, Gender = gender }, ct);
        return Ok(result);
    }
}
