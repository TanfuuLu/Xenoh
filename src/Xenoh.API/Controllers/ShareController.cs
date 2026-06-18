using Mediator;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/share")]
[EnableCors("PublicSharePolicy")]
public sealed class ShareController(
    IMediator mediator,
    IPrShareImageService imageService,
    IPrShareImageStorage imageStorage) : ControllerBase
{
    /// <summary>
    /// Serves the PR achievement card. The card is rendered once per PR version,
    /// stored in the public R2 share-images folder, and subsequently served straight
    /// from R2 via redirect. Pass ?copy=... to receive the raw PNG bytes instead
    /// (used by the clipboard flow, which needs a same-response CORS image).
    /// </summary>
    [HttpGet("pr/{userId:guid}/{exerciseTemplateId:guid}/image.png")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicShare)]
    public async Task<IActionResult> GetShareImage(
        Guid userId,
        Guid exerciseTemplateId,
        [FromQuery] string? copy,
        CancellationToken ct)
    {
        var data = await mediator.Send(new GetPrShareDataQuery(userId, exerciseTemplateId), ct);
        if (data is null) return NotFound();

        // R2 not configured (e.g. local dev): fall back to rendering inline.
        if (!imageStorage.IsConfigured)
        {
            var inline = await imageService.GenerateAsync(data, ct);
            Response.Headers.CacheControl = "public, max-age=3600";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            return File(inline, "image/png");
        }

        var url = await imageStorage.EnsureAsync(
            userId,
            exerciseTemplateId,
            data,
            token => imageService.GenerateAsync(data, token),
            ct);

        // Clipboard flow: proxy the stored PNG so the bytes arrive with permissive CORS.
        if (!string.IsNullOrEmpty(copy))
        {
            var bytes = await imageStorage.TryGetAsync(userId, exerciseTemplateId, data, ct)
                ?? await imageService.GenerateAsync(data, ct);
            Response.Headers.CacheControl = "public, max-age=3600";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            return File(bytes, "image/png");
        }

        // Display flow: hand the browser the public R2 URL. Keep the redirect short-lived
        // so a new PR (new versioned key) is picked up promptly.
        Response.Headers.CacheControl = "public, max-age=300";
        Response.Headers["Access-Control-Allow-Origin"] = "*";
        return Redirect(url);
    }
}
