using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xenoh.API.Controllers;

public abstract class CompetitionControllerBase : ControllerBase
{
    protected async Task<IActionResult> Send<T>(Func<ValueTask<T>> action, int successStatus = StatusCodes.Status200OK)
    {
        try { return StatusCode(successStatus, await action()); }
        catch (KeyNotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
        catch (UnauthorizedAccessException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden); }
        catch (DbUpdateConcurrencyException) { return Problem("The competition was changed by another user. Reload and try again.", statusCode: StatusCodes.Status409Conflict); }
        catch (InvalidOperationException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest); }
    }

    protected async Task<IActionResult> Send(Func<ValueTask<Unit>> action)
    {
        try { await action(); return NoContent(); }
        catch (KeyNotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
        catch (UnauthorizedAccessException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden); }
        catch (DbUpdateConcurrencyException) { return Problem("The competition was changed by another user. Reload and try again.", statusCode: StatusCodes.Status409Conflict); }
        catch (InvalidOperationException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest); }
    }
}

