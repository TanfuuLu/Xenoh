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

        var (statusCode, code, message) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("Request failed with {StatusCode} for {Method} {Path}: {Reason}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            code,
            message,
            traceId = context.TraceIdentifier,
        }, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Code, string Message) Map(Exception exception) => exception switch
    {
        AgreementConflictException e => (StatusCodes.Status409Conflict, "AGREEMENT_CONFLICT", ApiErrorMessages.Safe(e.Message, "The agreement changed. Reload and try again.")),
        Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505", ConstraintName: "IX_CoachClientRelationships_ClientId" or "IX_CoachingAgreements_RelationshipId_Version" or "IX_CoachInviteCodes_Code" } }
            => (StatusCodes.Status409Conflict, "CONFLICT", "The invitation or relationship changed. Reload and review the current agreement."),
        Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "CONFLICT", "The agreement changed. Reload and review the current version."),
        System.ComponentModel.DataAnnotations.ValidationException e => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ApiErrorMessages.Safe(e.Message, "Some fields are invalid. Check them and try again.")),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "FORBIDDEN", "You do not have access to this resource."),
        KeyNotFoundException e => (StatusCodes.Status404NotFound, "NOT_FOUND", ApiErrorMessages.Safe(e.Message, "The requested resource was not found.")),
        PaymentServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "PAYMENT_UNAVAILABLE", "Payment service is temporarily unavailable."),
        // SupplementConflictException derives from InvalidOperationException and is covered here.
        // Handlers use InvalidOperationException as the domain rule-violation type, and controllers
        // already surface its message as a 400; this is the fallback for the ones they miss.
        InvalidOperationException e => (StatusCodes.Status400BadRequest, "INVALID_OPERATION", ApiErrorMessages.Safe(e.Message, "That action could not be completed.")),
        ArgumentException e => (StatusCodes.Status400BadRequest, "INVALID_ARGUMENT", ApiErrorMessages.Safe(e.Message, "Some submitted data is invalid.")),
        // 499 Client Closed Request — the caller disconnected; not a server fault, so don't log it as one.
        OperationCanceledException => (499, "REQUEST_CANCELLED", "Request was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
    };
}
