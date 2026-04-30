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

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new InvalidOperationException("Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshToken = new TokenEntity
        {
            Token = refreshTokenValue,
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
}
