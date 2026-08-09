using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Competitions;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

// Authorization is per event, not per subscription: every action below resolves the caller's
// CompetitionStaffPermission flags for the target event inside its handler. Only event creation
// requires an active Organizer plan, so an owner's staff can operate the event for free and a
// lapsed owner can still service events that already have registered athletes.
[ApiController, Authorize, Route("api/competition-management")]
public sealed class CompetitionManagementController(IMediator mediator) : CompetitionControllerBase
{
    [HttpGet("events")]
    public Task<IActionResult> Events() => Send(() => mediator.Send(new GetManagedCompetitionEventsQuery()));
    [Authorize(Policy = SubscriptionPolicies.RequireOrganizer), HttpPost("events")]
    public Task<IActionResult> Create([FromBody] CompetitionEventInput input) => Send(() => mediator.Send(new CreateCompetitionEventCommand(input)), StatusCodes.Status201Created);
    [HttpPut("events/{eventId:guid}")]
    public Task<IActionResult> Update(Guid eventId, [FromBody] CompetitionEventInput input) => Send(() => mediator.Send(new UpdateCompetitionEventCommand(eventId, input)));
    [HttpDelete("events/{eventId:guid}")]
    public Task<IActionResult> Delete(Guid eventId) => Send(() => mediator.Send(new DeleteCompetitionEventCommand(eventId)));
    [HttpPost("events/{eventId:guid}/publish")]
    public Task<IActionResult> Publish(Guid eventId) => Send(() => mediator.Send(new PublishCompetitionEventCommand(eventId)));
    [HttpPost("events/{eventId:guid}/close-registration")]
    public Task<IActionResult> Close(Guid eventId) => Send(() => mediator.Send(new CloseCompetitionRegistrationCommand(eventId)));
    // Ending needs no justification, so the note is optional — unlike cancelling.
    [HttpPost("events/{eventId:guid}/end")]
    public Task<IActionResult> End(Guid eventId, [FromBody] NoteBody? body) => Send(() => mediator.Send(new EndCompetitionEventCommand(eventId, body?.Note)));
    [HttpPost("events/{eventId:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid eventId, [FromBody] ReasonBody body) => Send(() => mediator.Send(new CancelCompetitionEventCommand(eventId, body.Reason)));

    [HttpPost("events/{eventId:guid}/categories")]
    public Task<IActionResult> AddCategory(Guid eventId, [FromBody] CategoryInput input) => Send(() => mediator.Send(new UpsertCompetitionCategoryCommand(eventId, null, input)), StatusCodes.Status201Created);
    [HttpPut("events/{eventId:guid}/categories/{categoryId:guid}")]
    public Task<IActionResult> UpdateCategory(Guid eventId, Guid categoryId, [FromBody] CategoryInput input) => Send(() => mediator.Send(new UpsertCompetitionCategoryCommand(eventId, categoryId, input)));
    [HttpDelete("events/{eventId:guid}/categories/{categoryId:guid}")]
    public Task<IActionResult> DeleteCategory(Guid eventId, Guid categoryId) => Send(() => mediator.Send(new DeleteCompetitionCategoryCommand(eventId, categoryId)));
    [HttpDelete("events/{eventId:guid}/categories")]
    public Task<IActionResult> ClearCategories(Guid eventId) => Send(() => mediator.Send(new ClearCompetitionCategoriesCommand(eventId)));

    [HttpGet("events/{eventId:guid}/staff/lookup")]
    public Task<IActionResult> LookupStaff(Guid eventId, [FromQuery] string email) => Send(() => mediator.Send(new FindCompetitionStaffCandidateQuery(eventId, email)));
    [HttpPut("events/{eventId:guid}/staff/{userId:guid}")]
    public Task<IActionResult> SetStaff(Guid eventId, Guid userId, [FromBody] StaffBody body) => Send(() => mediator.Send(new SetCompetitionStaffCommand(eventId, userId, body.Permissions)));
    [HttpDelete("events/{eventId:guid}/staff/{userId:guid}")]
    public Task<IActionResult> RemoveStaff(Guid eventId, Guid userId) => Send(() => mediator.Send(new RemoveCompetitionStaffCommand(eventId, userId)));

    [HttpGet("events/{eventId:guid}/registrations")]
    public Task<IActionResult> Roster(Guid eventId, [FromQuery] CompetitionRegistrationStatus? status, [FromQuery] CompetitionPaymentStatus? paymentStatus,
        [FromQuery] Guid? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Send(() => mediator.Send(new GetCompetitionRosterQuery(eventId, status, paymentStatus, categoryId, page, pageSize)));
    [HttpPost("events/{eventId:guid}/registrations/guest")]
    public Task<IActionResult> Guest(Guid eventId, [FromBody] GuestBody body) => Send(() => mediator.Send(new AddGuestCompetitionRegistrationCommand(eventId,
        body.CategoryId, body.AthleteName, body.ContactEmail, body.ContactPhone, body.ContactFacebook)), StatusCodes.Status201Created);
    [HttpPost("events/{eventId:guid}/registrations/{registrationId:guid}/decision")]
    public Task<IActionResult> Decide(Guid eventId, Guid registrationId, [FromBody] DecisionBody body) => Send(() => mediator.Send(new DecideCompetitionRegistrationCommand(eventId, registrationId, body.Approve, body.Reason)));
    [HttpPost("events/{eventId:guid}/registrations/{registrationId:guid}/promote")]
    public Task<IActionResult> Promote(Guid eventId, Guid registrationId) => Send(() => mediator.Send(new PromoteCompetitionWaitlistCommand(eventId, registrationId)));
    [HttpPost("events/{eventId:guid}/registrations/{registrationId:guid}/link/{userId:guid}")]
    public Task<IActionResult> Link(Guid eventId, Guid registrationId, Guid userId) => Send(() => mediator.Send(new LinkGuestCompetitionRegistrationCommand(eventId, registrationId, userId)));

    [HttpPost("events/{eventId:guid}/receipts/{receiptId:guid}/decision")]
    public Task<IActionResult> ReviewReceipt(Guid eventId, Guid receiptId, [FromBody] DecisionBody body) => Send(() => mediator.Send(new ReviewCompetitionReceiptCommand(eventId, receiptId, body.Approve, body.Reason)));
    [HttpGet("events/{eventId:guid}/receipts/{receiptId:guid}/download")]
    public Task<IActionResult> Receipt(Guid eventId, Guid receiptId) => Send(() => mediator.Send(new GetCompetitionReceiptUrlQuery(eventId, receiptId)));

    [HttpPut("events/{eventId:guid}/results/powerlifting/{registrationId:guid}")]
    public Task<IActionResult> PowerResult(Guid eventId, Guid registrationId, [FromBody] PowerResultBody body) => Send(() => mediator.Send(new UpsertPowerliftingResultCommand(eventId,
        registrationId, body.BodyweightKg, body.BestSquatKg, body.BestBenchKg, body.BestDeadliftKg, body.State, body.Notes)));
    [HttpPut("events/{eventId:guid}/results/bodybuilding/{registrationId:guid}")]
    public Task<IActionResult> BodyResult(Guid eventId, Guid registrationId, [FromBody] BodyResultBody body) => Send(() => mediator.Send(new UpsertBodybuildingResultCommand(eventId, registrationId, body.Place, body.State, body.Notes)));
    [HttpPost("events/{eventId:guid}/results/publish")]
    public Task<IActionResult> PublishResults(Guid eventId) => Send(() => mediator.Send(new PublishCompetitionResultsCommand(eventId)));

    public sealed record ReasonBody(string Reason);
    public sealed record NoteBody(string? Note);
    public sealed record StaffBody(CompetitionStaffPermission Permissions);
    public sealed record DecisionBody(bool Approve, string? Reason);
    public sealed record GuestBody(Guid CategoryId, string AthleteName, string ContactEmail, string ContactPhone, string? ContactFacebook);
    public sealed record PowerResultBody(decimal BodyweightKg, decimal BestSquatKg, decimal BestBenchKg, decimal BestDeadliftKg, CompetitionResultState State, string? Notes);
    public sealed record BodyResultBody(int? Place, CompetitionResultState State, string? Notes);
}
