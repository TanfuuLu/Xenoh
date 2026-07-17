using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Competitions;

namespace Xenoh.API.Controllers;

[ApiController, Authorize(Policy = SubscriptionPolicies.RequireOrganizer), Route("api/organizers")]
public sealed class OrganizersController(IMediator mediator) : CompetitionControllerBase
{
    [HttpGet("me")]
    public Task<IActionResult> Mine() => Send(() => mediator.Send(new GetMyOrganizerProfileQuery()));

    [HttpPut("me/application")]
    public Task<IActionResult> Apply([FromBody] ApplyForOrganizerCommand command) => Send(() => mediator.Send(command));

    [HttpPost("me/evidence"), RequestSizeLimit(10 * 1024 * 1024)]
    public Task<IActionResult> Evidence(IFormFile file) => Send(async () =>
    {
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new UploadOrganizerEvidenceCommand(file.FileName, file.ContentType, file.Length, stream));
    }, StatusCodes.Status201Created);
}
