using Mediator;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions.Commands.HandleSePayWebhook;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IMediator mediator,
    ISePayWebhookVerifier webhookVerifier,
    ILogger<PaymentsController> logger
) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> SePayWebhook(
        [FromBody] SePayWebhookPayload payload, CancellationToken ct)
    {
        var authHeader = Request.Headers.Authorization.ToString();

        logger.LogInformation(
            "SePay webhook arrived - id={Id} amount={Amount} type={Type} code={Code} authPresent={AuthPresent}",
            payload.Id,
            payload.TransferAmount,
            payload.TransferType,
            payload.Code,
            !string.IsNullOrWhiteSpace(authHeader));

        if (!webhookVerifier.Verify(authHeader))
        {
            logger.LogWarning(
                "SePay webhook rejected - Apikey mismatch from {RemoteIp}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized();
        }

        var command = new HandleSePayWebhookCommand(
            payload.Id,
            payload.Gateway ?? string.Empty,
            payload.TransactionDate ?? string.Empty,
            payload.AccountNumber ?? string.Empty,
            payload.Code,
            payload.Content,
            payload.TransferType ?? string.Empty,
            payload.TransferAmount,
            payload.ReferenceCode,
            payload.Description);

        var result = await mediator.Send(command, ct);

        logger.LogInformation("SePay webhook result - id={Id} success={Success}", payload.Id, result.Success);

        return Ok(new { success = result.Success, message = result.Message });
    }
}

public sealed record SePayWebhookPayload(
    long Id,
    string? Gateway,
    string? TransactionDate,
    string? AccountNumber,
    string? SubAccId,
    string? Code,
    string? Content,
    string? TransferType,
    decimal TransferAmount,
    decimal Accumulated,
    string? ReferenceCode,
    string? Description
);
