using Mediator;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/share")]
[EnableCors("PublicSharePolicy")]
public sealed class ShareController(IMediator mediator, IPrShareImageService imageService) : ControllerBase
{
    /// <summary>
    /// Returns the PR achievement card as a 1200x630 PNG.
    /// </summary>
    [HttpGet("pr/{userId:guid}/{exerciseTemplateId:guid}/image.png")]
    public async Task<IActionResult> GetShareImage(Guid userId, Guid exerciseTemplateId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetPrShareDataQuery(userId, exerciseTemplateId), ct);
        if (data is null) return NotFound();

        var png = await imageService.GenerateAsync(data, ct);
        Response.Headers.CacheControl = "public, max-age=3600";
        // Keep the cached public PNG usable from the browser clipboard flow.
        Response.Headers["Access-Control-Allow-Origin"] = "*";
        return File(png, "image/png");
    }
}
