using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepo,
    ICurrentUserService currentUser,
    ITokenBlacklist tokenBlacklist
) : IRequestHandler<LogoutCommand>
{
    public async ValueTask<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var activeTokens = await refreshTokenRepo.GetActiveByUserAsync(userId, cancellationToken);

        foreach (var token in activeTokens)
            token.IsRevoked = true;

        if (activeTokens.Count > 0)
            await refreshTokenRepo.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.AccessToken))
            tokenBlacklist.RevokeToken(request.AccessToken);

        return Unit.Value;
    }
}
