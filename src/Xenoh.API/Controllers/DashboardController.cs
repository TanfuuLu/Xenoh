using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Dashboard.Queries.GetPersonalDashboard;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonal(CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPersonalDashboardQuery(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
