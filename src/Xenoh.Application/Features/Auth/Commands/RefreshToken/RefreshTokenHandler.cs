using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Auth.Commands.Register;
using Xenoh.Domain.Enums;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo,
    ISubscriptionRepository subscriptionRepo
) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokenRepo.FindActiveAsync(request.RefreshToken, cancellationToken)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (!storedToken.IsActive)
            throw new InvalidOperationException("Refresh token has expired or been revoked.");

        storedToken.IsRevoked = true;

        var user = storedToken.User;
        if (await userManager.IsLockedOutAsync(user))
            throw new InvalidOperationException("Account is suspended.");

        var roles = await userManager.GetRolesAsync(user);

        // Strip Coach role if ProCoach subscription has expired
        if (roles.Contains(UserRole.Coach))
        {
            var subscription = await subscriptionRepo.FindByUserIdAsync(user.Id, cancellationToken);
            var proCoachIsActive = subscription is not null
                && subscription.Tier == PlanTier.ProCoach
                && subscription.ExpiresAt.HasValue
                && subscription.ExpiresAt.Value > DateTime.UtcNow;

            if (!proCoachIsActive)
            {
                await userManager.RemoveFromRoleAsync(user, UserRole.Coach);
                roles = await userManager.GetRolesAsync(user);
            }
        }

        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var newRefreshTokenValue = tokenService.GenerateRefreshToken();

        var newRefreshToken = new TokenEntity
        {
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokenRepo.AddAsync(newRefreshToken, cancellationToken);
        await refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            accessToken,
            newRefreshTokenValue,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.AvatarUrl,
            roles
        );
    }
}
