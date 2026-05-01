using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Auth.Commands.ChangePassword;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;
using Xenoh.Application.Features.Auth.Commands.ForgotPassword;
using Xenoh.Application.Features.Auth.Commands.Login;
using Xenoh.Application.Features.Auth.Commands.Logout;
using Xenoh.Application.Features.Auth.Commands.RefreshToken;
using Xenoh.Application.Features.Auth.Commands.Register;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
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

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
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

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var accessToken = "";
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            accessToken = authHeader.Substring("Bearer ".Length).Trim();
        }

        var command = new LogoutCommand { AccessToken = accessToken };
        await mediator.Send(command, ct);
        await HttpContext.SignOutAsync("External");
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        try
        {
            await mediator.Send(command, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password/send-code")]
    public async Task<IActionResult> SendForgotPasswordCode([FromBody] SendForgotPasswordCodeCommand command, CancellationToken ct)
    {
        try
        {
            await mediator.Send(command, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithCodeCommand command, CancellationToken ct)
    {
        try
        {
            await mediator.Send(command, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("external/{provider}")]
    public IActionResult ExternalLogin([FromRoute] string provider)
    {
        var scheme = provider.ToLowerInvariant() switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "facebook" => FacebookDefaults.AuthenticationScheme,
            _ => null
        };

        if (scheme is null)
            return BadRequest(new { message = "Unsupported external login provider." });

        var properties = new AuthenticationProperties();
        if (scheme == GoogleDefaults.AuthenticationScheme)
        {
            properties.Parameters["prompt"] = "select_account";
        }
        else if (scheme == FacebookDefaults.AuthenticationScheme)
        {
            properties.Parameters["auth_type"] = "reauthenticate";
        }

        return Challenge(properties, scheme);
    }

    [HttpPost("external/exchange")]
    public async Task<IActionResult> ExchangeExternalLoginTicket(
        [FromBody] ExchangeExternalLoginTicketCommand command,
        CancellationToken ct)
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

    [HttpPost("external/complete-registration")]
    [Authorize]
    public async Task<IActionResult> CompleteExternalRegistration(
        [FromBody] CompleteExternalRegistrationCommand command,
        CancellationToken ct)
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
}
