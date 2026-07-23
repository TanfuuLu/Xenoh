using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.API.Auth;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Features.Supplements;
using Xenoh.Application.Features.Supplements.Commands;
using Xenoh.Application.Features.Supplements.Queries;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/supplements")]
[Authorize(Policy = SubscriptionPolicies.RequirePro)]
public sealed class SupplementsController(IMediator mediator) : ControllerBase
{
    [HttpGet("regimens")]
    public Task<IActionResult> GetRegimens(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementRegimensQuery(includeArchived),
                cancellationToken));

    [HttpPost("regimens")]
    public Task<IActionResult> CreateRegimen(
        [FromBody] CreateSupplementRegimenRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await mediator.Send(
                new CreateSupplementRegimenCommand(request),
                cancellationToken),
            created: true);

    [HttpPut("regimens/{regimenId:guid}")]
    public Task<IActionResult> UpdateRegimen(
        Guid regimenId,
        [FromBody] UpdateSupplementRegimenRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new UpdateSupplementRegimenCommand(regimenId, request),
                cancellationToken));

    [HttpDelete("regimens/{regimenId:guid}")]
    public Task<IActionResult> ArchiveRegimen(
        Guid regimenId,
        CancellationToken cancellationToken) =>
        ExecuteNoContentAsync(
            () => mediator.Send(
                new ArchiveSupplementRegimenCommand(regimenId),
                cancellationToken));

    [HttpGet("daily")]
    public Task<IActionResult> GetDaily(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementDailyQuery(date),
                cancellationToken));

    [HttpPut("doses/{doseSlotId:guid}/{date}")]
    public Task<IActionResult> RecordDose(
        Guid doseSlotId,
        DateOnly date,
        [FromBody] RecordSupplementDoseRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new RecordSupplementDoseCommand(doseSlotId, date, request),
                cancellationToken));

    [HttpDelete("doses/{doseSlotId:guid}/{date}")]
    public Task<IActionResult> ResetDose(
        Guid doseSlotId,
        DateOnly date,
        CancellationToken cancellationToken) =>
        ExecuteNoContentAsync(
            () => mediator.Send(
                new ResetSupplementDoseCommand(doseSlotId, date),
                cancellationToken));

    [HttpGet("history")]
    public Task<IActionResult> GetHistory(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementHistoryQuery(from, to),
                cancellationToken));

    [HttpGet("clients/{clientId:guid}/regimens")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> GetClientRegimens(
        Guid clientId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementRegimensQuery(includeArchived, clientId),
                cancellationToken));

    [HttpPost("clients/{clientId:guid}/regimens")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> CreateClientRegimen(
        Guid clientId,
        [FromBody] CreateSupplementRegimenRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await mediator.Send(
                new CreateSupplementRegimenCommand(request, clientId),
                cancellationToken),
            created: true);

    [HttpPut("clients/{clientId:guid}/regimens/{regimenId:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> UpdateClientRegimen(
        Guid clientId,
        Guid regimenId,
        [FromBody] UpdateSupplementRegimenRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new UpdateSupplementRegimenCommand(regimenId, request, clientId),
                cancellationToken));

    [HttpDelete("clients/{clientId:guid}/regimens/{regimenId:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> ArchiveClientRegimen(
        Guid clientId,
        Guid regimenId,
        CancellationToken cancellationToken) =>
        ExecuteNoContentAsync(
            () => mediator.Send(
                new ArchiveSupplementRegimenCommand(regimenId, clientId),
                cancellationToken));

    [HttpGet("clients/{clientId:guid}/daily")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> GetClientDaily(
        Guid clientId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementDailyQuery(date, clientId),
                cancellationToken));

    [HttpGet("clients/{clientId:guid}/history")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public Task<IActionResult> GetClientHistory(
        Guid clientId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => mediator.Send(
                new GetSupplementHistoryQuery(from, to, clientId),
                cancellationToken));

    private async Task<IActionResult> ExecuteAsync<T>(
        Func<ValueTask<T>> action,
        bool created = false)
    {
        try
        {
            var result = await action();
            return created ? StatusCode(StatusCodes.Status201Created, result) : Ok(result);
        }
        catch (SupplementConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<IActionResult> ExecuteNoContentAsync(
        Func<ValueTask<Unit>> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (SupplementConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
