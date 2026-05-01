using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
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

        return Unit.Value;
    }
}
