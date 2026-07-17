using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Competitions;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/organizers")]
public sealed class AdminOrganizersController(IMediator mediator) : CompetitionControllerBase
{
    [HttpGet]
    public Task<IActionResult> List([FromQuery] OrganizerProfileStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Send(() => mediator.Send(new GetOrganizerApplicationsQuery(status, page, pageSize)));
    [HttpPost("{profileId:guid}/decision")]
    public Task<IActionResult> Review(Guid profileId, [FromBody] DecisionBody body) => Send(() => mediator.Send(new ReviewOrganizerApplicationCommand(profileId, body.Decision, body.Reason)));
    [HttpGet("{profileId:guid}/evidence")]
    public Task<IActionResult> Evidence(Guid profileId) => Send(() => mediator.Send(new GetOrganizerEvidenceUrlQuery(profileId)));
    [HttpGet("events/{eventId:guid}/receipts/{receiptId:guid}/download")]
    public Task<IActionResult> Receipt(Guid eventId, Guid receiptId) => Send(() => mediator.Send(new GetCompetitionReceiptUrlQuery(eventId, receiptId, true)));
    public sealed record DecisionBody(OrganizerProfileStatus Decision, string Reason);
}
