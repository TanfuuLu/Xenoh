using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Coaches.Queries.GetCoaches;

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
}
