using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Admin;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Reports.Commands.SetUserSuspension;

public sealed class SetUserSuspensionHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IRefreshTokenRepository refreshTokenRepo,
    IApplicationDbContext db)
    : IRequestHandler<SetUserSuspensionCommand>
{
    public async ValueTask<Unit> Handle(SetUserSuspensionCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var beforeSuspended = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(
            user,
            request.Suspended ? DateTimeOffset.UtcNow.AddYears(100) : null);

        if (!result.Succeeded)
            throw new InvalidOperationException("Could not update user suspension.");

        // Lockout only blocks new logins; without this a suspended user keeps refreshing
        // their session until token expiry. Revoke all active refresh tokens on suspend.
        if (request.Suspended)
        {
            var activeTokens = await refreshTokenRepo.GetActiveByUserAsync(user.Id, cancellationToken);
            foreach (var token in activeTokens)
                token.IsRevoked = true;
            if (activeTokens.Count > 0)
                await refreshTokenRepo.SaveChangesAsync(cancellationToken);
        }

        AdminAudit.Add(
            db,
            currentUser.UserId,
            request.Suspended ? AdminAudit.SuspendUser : AdminAudit.UnsuspendUser,
            nameof(ApplicationUser),
            user.Id,
            user.Id,
            request.Suspended ? "Admin suspended user." : "Admin unsuspended user.",
            $"Suspended={beforeSuspended}",
            $"Suspended={request.Suspended}");
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
