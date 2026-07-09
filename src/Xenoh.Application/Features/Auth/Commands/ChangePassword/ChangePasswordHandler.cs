using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IRefreshTokenRepository refreshTokenRepo,
    ITokenBlacklist tokenBlacklist
) : IRequestHandler<ChangePasswordCommand>
{
    public async ValueTask<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Revoke every existing session so a stolen refresh token cannot outlive the
        // password change (refresh tokens otherwise stay valid for 7 days).
        var activeTokens = await refreshTokenRepo.GetActiveByUserAsync(user.Id, cancellationToken);
        if (activeTokens.Count > 0)
        {
            foreach (var token in activeTokens)
                token.IsRevoked = true;
            await refreshTokenRepo.SaveChangesAsync(cancellationToken);
        }

        // Kill the caller's current access token immediately (rather than at its ≤60-min
        // expiry) so the change takes effect on this device without a re-login race.
        if (!string.IsNullOrEmpty(request.AccessToken))
            await tokenBlacklist.RevokeTokenAsync(request.AccessToken);

        return Unit.Value;
    }
}
