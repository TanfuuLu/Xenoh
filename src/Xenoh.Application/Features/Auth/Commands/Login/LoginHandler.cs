using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Auth.Commands.Register;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo
) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (await userManager.IsLockedOutAsync(user))
            throw new InvalidOperationException(LockoutMessage(user));

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            // CheckPasswordAsync does not touch the failure counter on its own. Without this the
            // account never locks and password guessing is bounded only by the per-IP rate limit,
            // which an attacker rotating source IPs sidesteps entirely.
            if (!await userManager.GetLockoutEnabledAsync(user))
                await userManager.SetLockoutEnabledAsync(user, true);

            await userManager.AccessFailedAsync(user);

            throw new InvalidOperationException(await userManager.IsLockedOutAsync(user)
                ? LockoutMessage(user)
                : "Invalid email or password.");
        }

        // No-ops when the count is already zero, so successful logins stay a single read.
        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshToken = new TokenEntity
        {
            Token = tokenService.HashRefreshToken(refreshTokenValue),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokenRepo.AddAsync(refreshToken, cancellationToken);
        await refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            accessToken,
            refreshTokenValue,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.AvatarUrl,
            roles
        );
    }

    // Admin suspension parks LockoutEnd 100 years out (SetUserSuspensionHandler); a brute-force
    // lockout only lasts DefaultLockoutTimeSpan. The gap keeps the two messages distinguishable.
    private static string LockoutMessage(ApplicationUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1)
            ? "Account is suspended."
            : "Too many failed login attempts. Please try again later.";
}
