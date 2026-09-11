using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.API.Auth;
using Xenoh.API.Security;
using Xenoh.Application.Features.ProgressLibrary.Commands.CreateProgressCheckIn;
using Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressCheckIn;
using Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressPhoto;
using Xenoh.Application.Features.ProgressLibrary.Commands.SetProgressCheckInSharing;
using Xenoh.Application.Features.ProgressLibrary.Commands.UpdateProgressCheckIn;
using Xenoh.Application.Features.ProgressLibrary.Dtos;
using Xenoh.Application.Features.ProgressLibrary.Queries.GetProgressPhotoPreviewUrl;
using Xenoh.Application.Features.ProgressLibrary.Queries.ListClientProgressCheckIns;
using Xenoh.Application.Features.ProgressLibrary.Queries.ListMyProgressCheckIns;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/progress-library")]
[Authorize]
public sealed class ProgressLibraryController(IMediator mediator) : ControllerBase
{
    private const long MaxRequestBytes = ProgressLibraryRules.MaximumPhotoSizeBytes * ProgressLibraryRules.MaximumPhotosPerCheckIn;

    public sealed record UpdateCheckInRequest(DateOnly CheckInDate, string? Title, string? Notes, decimal? BodyweightKg, bool IsMilestone);
    public sealed record SharingRequest(bool IsSharedWithCoach);

    [HttpGet]
    public async Task<IActionResult> ListMine(CancellationToken ct) => Ok(await mediator.Send(new ListMyProgressCheckInsQuery(), ct));

    [HttpGet("clients/{clientId:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> ListClient(Guid clientId, CancellationToken ct) =>
        Ok(await mediator.Send(new ListClientProgressCheckInsQuery(clientId), ct));

    [HttpPost]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(
        [FromForm] DateOnly checkInDate,
        [FromForm] string? title,
        [FromForm] string? notes,
        [FromForm] decimal? bodyweightKg,
        [FromForm] bool isMilestone,
        [FromForm] List<IFormFile> photos,
        [FromForm] List<ProgressPhotoAngle> angles,
        CancellationToken ct)
    {
        if (photos.Count != angles.Count)
            return BadRequest(new { message = "Each photo needs an angle." });
        if (!ProgressLibraryRules.HasValidPhotoCount(photos.Count))
            return BadRequest(new { message = "Add between 1 and 3 photos to each check-in." });

        try
        {
            var uploads = new List<ProgressPhotoUpload>();
            for (var i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                if (photo.Length <= 0)
                    return BadRequest(new { message = "Each photo is required." });
                uploads.Add(new ProgressPhotoUpload(photo.FileName, photo.ContentType, photo.Length, angles[i], photo.OpenReadStream()));
            }

            await using var streams = new AsyncDisposer(uploads.Select(x => x.Content));
            return Ok(await mediator.Send(new CreateProgressCheckInCommand(checkInDate, title, notes, bodyweightKg, isMilestone, uploads), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ApiErrorMessages.Safe(ex.Message, "The check-in could not be saved.") });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCheckInRequest body, CancellationToken ct) =>
        await TrySendAsync(new UpdateProgressCheckInCommand(id, body.CheckInDate, body.Title, body.Notes, body.BodyweightKg, body.IsMilestone), ct);

    [HttpPut("{id:guid}/sharing")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> SetSharing(Guid id, [FromBody] SharingRequest body, CancellationToken ct) =>
        await TrySendAsync(new SetProgressCheckInSharingCommand(id, body.IsSharedWithCoach), ct);

    [HttpGet("photos/{photoId:guid}/preview-url")]
    public async Task<IActionResult> GetPreviewUrl(Guid photoId, CancellationToken ct) =>
        await TrySendAsync(new GetProgressPhotoPreviewUrlQuery(photoId), ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteProgressCheckInCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ApiErrorMessages.Safe(ex.Message, "The check-in could not be deleted.") });
        }
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteProgressPhotoCommand(id, photoId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ApiErrorMessages.Safe(ex.Message, "The photo could not be deleted.") });
        }
    }

    private async Task<IActionResult> TrySendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ApiErrorMessages.Safe(ex.Message, "The request could not be completed.") });
        }
    }

    private sealed class AsyncDisposer(IEnumerable<Stream> streams) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }
}
