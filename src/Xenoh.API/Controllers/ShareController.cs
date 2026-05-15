using Mediator;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/share")]
public sealed class ShareController(IMediator mediator, IPrShareImageService imageService) : ControllerBase
{
    /// <summary>
    /// Public OG meta-tag page for a user's PR. Facebook/Twitter scrapers hit this to build the card preview.
    /// Immediately redirects visitors to xenoh.online.
    /// </summary>
    [HttpGet("pr/{userId:guid}/{exerciseTemplateId:guid}")]
    [ResponseCache(Duration = 0, NoStore = true)]
    public async Task<IActionResult> GetSharePage(Guid userId, Guid exerciseTemplateId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetPrShareDataQuery(userId, exerciseTemplateId), ct);
        if (data is null) return NotFound();

        var imageUrl = $"{Request.Scheme}://{Request.Host}/api/share/pr/{userId}/{exerciseTemplateId}/image.png";
        var pageUrl  = $"{Request.Scheme}://{Request.Host}/api/share/pr/{userId}/{exerciseTemplateId}";

        var title       = HtmlEncode($"{data.UserName} hit a new PR! 🏆");
        var description = HtmlEncode($"{data.ExerciseName}: {data.WeightKg:0.##} kg × {data.Reps} rep{(data.Reps != 1 ? "s" : "")}");

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta property="og:title"        content="{title}" />
              <meta property="og:description"  content="{description}" />
              <meta property="og:image"        content="{imageUrl}" />
              <meta property="og:image:type"   content="image/png" />
              <meta property="og:image:width"  content="1200" />
              <meta property="og:image:height" content="630" />
              <meta property="og:type"         content="website" />
              <meta property="og:url"          content="{pageUrl}" />
              <meta property="og:site_name"    content="Xenoh" />
              <meta name="twitter:card"        content="summary_large_image" />
              <meta name="twitter:title"       content="{title}" />
              <meta name="twitter:description" content="{description}" />
              <meta name="twitter:image"       content="{imageUrl}" />
              <title>{title}</title>
            </head>
            <body>
              <script>window.location.replace("https://xenoh.online");</script>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    /// <summary>
    /// Returns the PR achievement card as a 1200×630 PNG.
    /// </summary>
    [HttpGet("pr/{userId:guid}/{exerciseTemplateId:guid}/image.png")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetShareImage(Guid userId, Guid exerciseTemplateId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetPrShareDataQuery(userId, exerciseTemplateId), ct);
        if (data is null) return NotFound();

        var png = await imageService.GenerateAsync(data, ct);
        return File(png, "image/png");
    }

    private static string HtmlEncode(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
