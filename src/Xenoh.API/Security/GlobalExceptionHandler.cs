using Microsoft.AspNetCore.Diagnostics;
using Xenoh.Application.Common.Exceptions;

namespace Xenoh.API.Security;

/// <summary>
/// Maps unhandled exceptions to the API's standard <c>{ "message": ... }</c> error shape.
/// Without this, authorization failures thrown deep in handlers surfaced as HTTP 500 — clients
/// could not distinguish "denied" from "server broke", and every probe raised an error alert.
/// Only the mapped exception types expose their message; anything else returns a generic 500
/// so internal detail never reaches the client.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
            return false;

        var (statusCode, message) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("Request failed with {StatusCode} for {Method} {Path}: {Reason}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Message) Map(Exception exception) => exception switch
    {
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "You do not have access to this resource."),
        KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
        PaymentServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Payment service is temporarily unavailable."),
        // SupplementConflictException derives from InvalidOperationException and is covered here.
        // Handlers use InvalidOperationException as the domain rule-violation type, and controllers
        // already surface its message as a 400; this is the fallback for the ones they miss.
        InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),
        ArgumentException e => (StatusCodes.Status400BadRequest, e.Message),
        // 499 Client Closed Request — the caller disconnected; not a server fault, so don't log it as one.
        OperationCanceledException => (499, "Request was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
