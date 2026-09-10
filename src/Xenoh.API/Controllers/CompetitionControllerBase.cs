using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xenoh.API.Security;

namespace Xenoh.API.Controllers;

public abstract class CompetitionControllerBase : ControllerBase
{
    protected async Task<IActionResult> Send<T>(Func<ValueTask<T>> action, int successStatus = StatusCodes.Status200OK)
    {
        try { return StatusCode(successStatus, await action()); }
        catch (KeyNotFoundException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "The requested competition resource was not found."), statusCode: StatusCodes.Status404NotFound); }
        catch (UnauthorizedAccessException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "You do not have access to this competition."), statusCode: StatusCodes.Status403Forbidden); }
        catch (DbUpdateConcurrencyException) { return Problem("The competition was changed by another user. Reload and try again.", statusCode: StatusCodes.Status409Conflict); }
        catch (InvalidOperationException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "That competition action could not be completed."), statusCode: StatusCodes.Status400BadRequest); }
    }

    protected async Task<IActionResult> Send(Func<ValueTask<Unit>> action)
    {
        try { await action(); return NoContent(); }
        catch (KeyNotFoundException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "The requested competition resource was not found."), statusCode: StatusCodes.Status404NotFound); }
        catch (UnauthorizedAccessException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "You do not have access to this competition."), statusCode: StatusCodes.Status403Forbidden); }
        catch (DbUpdateConcurrencyException) { return Problem("The competition was changed by another user. Reload and try again.", statusCode: StatusCodes.Status409Conflict); }
        catch (InvalidOperationException ex) { return Problem(ApiErrorMessages.Safe(ex.Message, "That competition action could not be completed."), statusCode: StatusCodes.Status400BadRequest); }
    }
}
