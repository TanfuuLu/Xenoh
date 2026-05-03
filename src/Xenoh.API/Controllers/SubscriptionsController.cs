using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Subscriptions.Commands.CreatePaymentOrder;
using Xenoh.Application.Features.Subscriptions.Commands.DevActivateSubscription;
using Xenoh.Application.Features.Subscriptions.Queries.GetMySubscription;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public sealed class SubscriptionsController(IMediator mediator, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMySubscriptionQuery(), ct);
        return Ok(result);
    }

    [HttpPost("payment-orders")]
    public async Task<IActionResult> CreatePaymentOrder(
        [FromBody] CreatePaymentOrderCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DEV ONLY — Activates the most recent pending payment order for the current user
    /// without requiring a SePay webhook. Use this when testing locally (SePay can't
    /// reach localhost). Returns 404 in non-Development environments.
    /// </summary>
    [HttpPost("dev-activate")]
    public async Task<IActionResult> DevActivate(CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var result = await mediator.Send(new DevActivateSubscriptionCommand(), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
