using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepo,
    ICurrentUserService currentUser,
    ITokenBlacklist tokenBlacklist,
    ITokenService tokenService
) : IRequestHandler<LogoutCommand>
{
    public async ValueTask<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var revokedAnyRefreshToken = false;
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshTokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var refreshToken = await refreshTokenRepo.FindActiveAsync(refreshTokenHash, cancellationToken);
            if (refreshToken is not null && refreshToken.UserId == currentUser.UserId)
            {
                refreshToken.IsRevoked = true;
                revokedAnyRefreshToken = true;
            }
        }
        else
        {
            var userId = currentUser.UserId;
            var activeTokens = await refreshTokenRepo.GetActiveByUserAsync(userId, cancellationToken);
            foreach (var token in activeTokens)
                token.IsRevoked = true;
            revokedAnyRefreshToken = activeTokens.Count > 0;
        }

        if (revokedAnyRefreshToken)
            await refreshTokenRepo.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.AccessToken))
            tokenBlacklist.RevokeToken(request.AccessToken);

        return Unit.Value;
    }
}
