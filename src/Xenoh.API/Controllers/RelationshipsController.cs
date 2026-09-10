using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.CoachClient.Agreements;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/relationships")]
[Authorize]
public sealed class RelationshipsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, CancellationToken ct = default) =>
        Ok(await mediator.Send(new ListAgreementsQuery(page), ct));

    [HttpPost("invitation-preview")]
    public async Task<IActionResult> Preview(PreviewInvitationBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new PreviewAgreementQuery(body.Code), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await mediator.Send(new GetAgreementQuery(id), ct));

    [HttpGet("{id:guid}/ending-preview")]
    public async Task<IActionResult> PreviewEnding(Guid id, [FromQuery] bool immediate, CancellationToken ct) =>
        Ok(await mediator.Send(new PreviewEndingQuery(id, immediate), ct));

    [HttpPost("{id:guid}/ending")]
    public async Task<IActionResult> End(Guid id, EndRelationshipCommand body, CancellationToken ct)
    {
        await mediator.Send(body with { RelationshipId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/ending-response")]
    public async Task<IActionResult> RespondToEnding(Guid id, EndingResponseBody body, CancellationToken ct)
    {
        await mediator.Send(new RespondToCoachEndingCommand(id, body.ExpectedRevision, body.Accept), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/proposals")]
    public async Task<IActionResult> Propose(Guid id, ProposeAgreementCommand body, CancellationToken ct) =>
        Ok(await mediator.Send(body with { RelationshipId = id }, ct));

    [HttpPost("{id:guid}/proposals/{agreementId:guid}/response")]
    public async Task<IActionResult> Respond(Guid id, Guid agreementId, AgreementResponseBody body, CancellationToken ct)
    {
        await mediator.Send(new RespondToAgreementCommand(id, agreementId, body.ExpectedRevision, body.Accept, body.Acknowledged), ct);
        return NoContent();
    }

    public sealed record PreviewInvitationBody([param: System.ComponentModel.DataAnnotations.Required] string Code);
    public sealed record EndingResponseBody(Guid ExpectedRevision, bool Accept);
    public sealed record AgreementResponseBody(Guid ExpectedRevision, bool Accept, bool Acknowledged);
}
