using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.CoachRatings.Commands.CreateCoachRating;
using Xenoh.Application.Features.CoachRatings.Commands.DeleteCoachRating;
using Xenoh.Application.Features.CoachRatings.Commands.UpdateCoachRating;
using Xenoh.Application.Features.Coaches.Queries.GetCoaches;
using Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/coaches")]
[Authorize]
public sealed class CoachesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách coach, có thể tìm kiếm theo tên.
    /// </summary>
    /// <param name="name">Optional — tìm theo first name, last name, hoặc full name (không phân biệt hoa thường)</param>
    [HttpGet]
    public async Task<IActionResult> GetCoaches([FromQuery] string? name, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoachesQuery(name), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a coach's public profile (name, email, total clients).
    /// Any authenticated user can view.
    /// </summary>
    [HttpGet("{coachId:guid}")]
    public async Task<IActionResult> GetCoachProfile(Guid coachId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetCoachProfileQuery(coachId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{coachId:guid}/rating")]
    public async Task<IActionResult> CreateRating(Guid coachId, [FromBody] CreateCoachRatingCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { CoachId = coachId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{coachId:guid}/rating")]
    public async Task<IActionResult> UpdateRating(Guid coachId, [FromBody] UpdateCoachRatingCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { CoachId = coachId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{coachId:guid}/rating")]
    public async Task<IActionResult> DeleteRating(Guid coachId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteCoachRatingCommand(coachId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
