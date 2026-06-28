using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Files.Commands.DeleteFile;
using Xenoh.Application.Features.Files.Commands.ShareFileWithClient;
using Xenoh.Application.Features.Files.Commands.UnshareFile;
using Xenoh.Application.Features.Files.Commands.UploadFile;
using Xenoh.Application.Features.Files.Queries.GetFileDownloadUrl;
using Xenoh.Application.Features.Files.Queries.ListMyFiles;
using Xenoh.Application.Features.Files.Queries.ListSharedWithMe;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize(Policy = SubscriptionPolicies.RequirePro)]
public sealed class FilesController(IMediator mediator) : ControllerBase
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    /// <summary>Upload a PDF or Word document to the user's storage.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken ct)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "File is required." });

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(new UploadFileCommand(
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

    /// <summary>The current user's files plus quota usage.</summary>
    [HttpGet]
    public async Task<IActionResult> ListMyFiles(CancellationToken ct)
    {
        var result = await mediator.Send(new ListMyFilesQuery(), ct);
        return Ok(result);
    }

    /// <summary>Files a coach has shared with the current user.</summary>
    [HttpGet("shared-with-me")]
    public async Task<IActionResult> ListSharedWithMe(CancellationToken ct)
    {
        var result = await mediator.Send(new ListSharedWithMeQuery(), ct);
        return Ok(result);
    }

    /// <summary>A short-lived presigned download URL (owner or share recipient).</summary>
    [HttpGet("{id:guid}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetFileDownloadUrlQuery(id), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteFileCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed record ShareFileRequest(Guid ClientId);

    /// <summary>[Coach only] Share one of the coach's files with an active client.</summary>
    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareFileRequest body, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ShareFileWithClientCommand(id, body.ClientId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/share/{shareId:guid}")]
    public async Task<IActionResult> Unshare(Guid id, Guid shareId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UnshareFileCommand(id, shareId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
