using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ITokenBlacklist tokenBlacklist
) : IRequestHandler<LogoutCommand>
{
    public async ValueTask<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        
        // Revoke all active refresh tokens for the current user
        var activeTokens = await context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        if (activeTokens.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Also revoke the current access token
        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            tokenBlacklist.RevokeToken(request.AccessToken);
        }

        return Unit.Value;
    }
}
