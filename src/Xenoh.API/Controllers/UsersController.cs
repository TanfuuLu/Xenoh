using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Users.Commands.DeleteBodyweightEntry;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;
using Xenoh.Application.Features.Users.Commands.UpdateMyAvatar;
using Xenoh.Application.Features.Users.Commands.UpdateMyProfile;
using Xenoh.Application.Features.Users.Commands.UpdateMyPreferences;
using Xenoh.Application.Features.Reports.Commands.CreateUserReport;
using Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;
using Xenoh.Application.Features.Users.Queries.GetExercisePrHistory;
using Xenoh.Application.Features.Users.Queries.GetExercisePrs;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Application.Features.Users.Queries.GetMyPreferences;
using Xenoh.Application.Features.Users.Queries.GetMyTrainingActivity;
using Xenoh.Application.Features.Users.Queries.GetPublicUserProfile;
using Xenoh.Application.Features.Users.Queries.GetUserProfile;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command, CancellationToken ct)
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

    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetMyPreferences(CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetMyPreferencesQuery(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdateMyPreferences([FromBody] UpdateMyPreferencesCommand command, CancellationToken ct)
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

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UpdateMyAvatar([FromForm] IFormFile? file, CancellationToken ct)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Avatar image is required." });

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(new UpdateMyAvatarCommand(
                file.FileName,
                file.ContentType,
                file.Length,
                stream), ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/bodyweight")]
    public async Task<IActionResult> LogBodyweight([FromBody] LogBodyweightCommand command, CancellationToken ct)
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

    [HttpGet("me/bodyweight")]
    public async Task<IActionResult> GetBodyweightHistory(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBodyweightHistoryQuery(), ct);
        return Ok(result);
    }

    [HttpGet("me/exercise-prs")]
    public async Task<IActionResult> GetExercisePrs(CancellationToken ct)
    {
        var result = await mediator.Send(new GetExercisePrsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("me/exercise-prs/{exerciseTemplateId:guid}/history")]
    public async Task<IActionResult> GetExercisePrHistory(Guid exerciseTemplateId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetExercisePrHistoryQuery(exerciseTemplateId), ct);
        return Ok(result);
    }

    [HttpGet("me/training-activity")]
    public async Task<IActionResult> GetMyTrainingActivity([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetMyTrainingActivityQuery(year, month), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{userId:guid}/bodyweight")]
    public async Task<IActionResult> GetUserBodyweightHistory(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetBodyweightHistoryQuery(userId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("me/bodyweight/{id:guid}")]
    public async Task<IActionResult> DeleteBodyweightEntry(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteBodyweightEntryCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{userId:guid}/public")]
    public async Task<IActionResult> GetPublicUserProfile(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPublicUserProfileQuery(userId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Coach views a client's full profile stats, or a client views their coach's profile.
    /// Requires an active coach–client relationship between caller and the target user.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetUserProfileQuery(userId), ct);
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

    [HttpPost("{userId:guid}/reports")]
    public async Task<IActionResult> ReportUser(Guid userId, [FromBody] CreateUserReportCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { ReportedUserId = userId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
