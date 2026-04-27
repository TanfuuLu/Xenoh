using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;
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

    /// <summary>
    /// Returns top 50 users ranked by Big 3 total (Squat + Bench + Deadlift PRs).
    /// gender: male | female (optional filter)
    /// </summary>
    [HttpGet("big3")]
    public async Task<IActionResult> GetBig3(
        [FromQuery] string? gender = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetBig3LeaderboardQuery { Gender = gender }, ct);
        return Ok(result);
    }
}
