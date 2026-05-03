using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Reports.Commands.SetUserSuspension;

public sealed class SetUserSuspensionHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<SetUserSuspensionCommand>
{
    public async ValueTask<Unit> Handle(SetUserSuspensionCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(
            user,
            request.Suspended ? DateTimeOffset.UtcNow.AddYears(100) : null);

        if (!result.Succeeded)
            throw new InvalidOperationException("Could not update user suspension.");

        return Unit.Value;
    }
}
