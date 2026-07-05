using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.Application.Features.Auth.Commands.ChangePassword;
using Xenoh.Application.Features.Auth.Commands.ExternalLogin;
using Xenoh.Application.Features.Auth.Commands.ForgotPassword;
using Xenoh.Application.Features.Auth.Commands.Login;
using Xenoh.Application.Features.Auth.Commands.Logout;
using Xenoh.Application.Features.Auth.Commands.RefreshToken;
using Xenoh.Application.Features.Auth.Commands.Register;
using Xenoh.Application.Features.WebsiteAnalytics;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator, ILogger<AuthController> logger) : ControllerBase
{
    private const string RefreshTokenCookieName = "xenoh.refresh";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicyNames.Auth)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            await TrackAuthEventAsync(WebsiteActivityEventType.Register, result.UserId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicyNames.Auth)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            await TrackAuthEventAsync(WebsiteActivityEventType.Login, result.UserId, ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ToAuthResponseBody(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimitPolicyNames.RefreshToken)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        try
        {
            if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) ||
                string.IsNullOrWhiteSpace(refreshToken))
                return BadRequest(new { message = "Invalid refresh token." });

            var command = new RefreshTokenCommand { RefreshToken = refreshToken };
            var result = await mediator.Send(command, ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ToAuthResponseBody(result));
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

        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken);

        var command = new LogoutCommand
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
        await mediator.Send(command, ct);
        await HttpContext.SignOutAsync("External");
        ClearRefreshTokenCookie();
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
    [EnableRateLimiting(RateLimitPolicyNames.Auth)]
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
    [EnableRateLimiting(RateLimitPolicyNames.Auth)]
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

    [HttpPost("forgot-password/verify-code")]
    [EnableRateLimiting(RateLimitPolicyNames.Auth)]
    public async Task<IActionResult> VerifyForgotPasswordCode([FromBody] VerifyForgotPasswordCodeCommand command, CancellationToken ct)
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
    [EnableRateLimiting(RateLimitPolicyNames.ExternalAuth)]
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
    [EnableRateLimiting(RateLimitPolicyNames.ExternalAuth)]
    public async Task<IActionResult> ExchangeExternalLoginTicket(
        [FromBody] ExchangeExternalLoginTicketCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ToAuthResponseBody(result));
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
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ToAuthResponseBody(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            MaxAge = RefreshTokenLifetime
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth"
        });
    }

    private static object ToAuthResponseBody(AuthResponse result) => new
    {
        result.UserId,
        result.AccessToken,
        result.Email,
        result.FullName,
        result.AvatarUrl,
        result.Roles
    };

    private async Task TrackAuthEventAsync(WebsiteActivityEventType eventType, Guid userId, CancellationToken ct)
    {
        var sessionId = Header("X-Xenoh-Session-Id");
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = $"auth-{userId:N}";

        try
        {
            await mediator.Send(new TrackWebsiteActivityCommand(
                eventType,
                sessionId,
                Header("X-Xenoh-Path") ?? Request.Path.Value ?? "/",
                Header("X-Xenoh-Previous-Path"),
                Header("X-Xenoh-Referrer"),
                Header("X-Xenoh-Utm-Source"),
                Header("X-Xenoh-Utm-Medium"),
                Header("X-Xenoh-Utm-Campaign"),
                null,
                Request.Headers.UserAgent.ToString(),
                userId), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to track auth event {EventType} for user {UserId}.",
                eventType,
                userId);
        }
    }

    private string? Header(string name)
    {
        var value = Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
