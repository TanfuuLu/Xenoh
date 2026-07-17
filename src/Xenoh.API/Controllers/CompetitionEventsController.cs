using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Competitions;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController, Route("api/events")]
public sealed class CompetitionEventsController(IMediator mediator) : CompetitionControllerBase
{
    [AllowAnonymous, HttpGet]
    public Task<IActionResult> List([FromQuery] CompetitionDiscipline? discipline, [FromQuery] CompetitionEventStatus? status,
        [FromQuery] DateTime? startsAfterUtc, [FromQuery] string? location, [FromQuery] string? cursor, [FromQuery] int pageSize = 20) =>
        Send(() => mediator.Send(new GetPublicCompetitionEventsQuery(discipline, status, startsAfterUtc, location, cursor, pageSize)));

    [AllowAnonymous, HttpGet("{slug}")]
    public Task<IActionResult> Get(string slug) => Send(() => mediator.Send(new GetCompetitionEventBySlugQuery(slug)));

    [AllowAnonymous, HttpGet("{slug}/categories")]
    public Task<IActionResult> Categories(string slug) => Send(async () =>
        (await mediator.Send(new GetCompetitionEventBySlugQuery(slug))).Categories);

    [AllowAnonymous, HttpGet("{slug}/results")]
    public Task<IActionResult> Results(string slug) => Send(() => mediator.Send(new GetCompetitionResultsQuery(slug)));

    [Authorize, HttpPost("{eventId:guid}/registrations")]
    public Task<IActionResult> Register(Guid eventId, [FromBody] RegisterBody body) => Send(() => mediator.Send(new RegisterForCompetitionCommand(eventId,
        body.CategoryId, body.ContactEmail, body.ContactPhone, body.ContactFacebook)), StatusCodes.Status201Created);

    [Authorize, HttpGet("registrations/me")]
    public Task<IActionResult> Mine() => Send(() => mediator.Send(new GetMyCompetitionRegistrationsQuery()));

    [Authorize, HttpGet("{eventId:guid}/registrations/me")]
    public Task<IActionResult> MyRegistration(Guid eventId) => Send(() => mediator.Send(new GetMyCompetitionRegistrationQuery(eventId)));

    [Authorize, HttpPost("{eventId:guid}/registrations/me/withdraw")]
    public Task<IActionResult> Withdraw(Guid eventId) => Send(() => mediator.Send(new WithdrawCompetitionRegistrationCommand(eventId)));

    [Authorize, HttpPost("{eventId:guid}/registrations/me/receipts")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<IActionResult> UploadReceipt(Guid eventId, IFormFile file) => Send(async () =>
    {
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new UploadCompetitionReceiptCommand(eventId, file.FileName, file.ContentType, file.Length, stream));
    }, StatusCodes.Status201Created);

    [Authorize, HttpGet("{eventId:guid}/receipts/{receiptId:guid}/download")]
    public Task<IActionResult> Receipt(Guid eventId, Guid receiptId) => Send(() => mediator.Send(new GetCompetitionReceiptUrlQuery(eventId, receiptId)));

    public sealed record RegisterBody(Guid CategoryId, string ContactEmail, string ContactPhone, string? ContactFacebook);
}
