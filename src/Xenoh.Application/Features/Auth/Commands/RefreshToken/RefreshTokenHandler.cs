using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Auth.Commands.Register;
using ApplicationUser = Xenoh.Domain.Entities.ApplicationUser;
using TokenEntity = Xenoh.Domain.Entities.RefreshToken;

namespace Xenoh.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IApplicationDbContext context
) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken)
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

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            newRefreshTokenValue,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            roles
        );
    }
}
