using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Auth.Commands.Register;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepo
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
        var roles = await userManager.GetRolesAsync(user);
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
