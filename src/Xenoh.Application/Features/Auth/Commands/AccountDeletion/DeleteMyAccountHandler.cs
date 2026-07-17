using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public sealed class DeleteMyAccountHandler(
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IAccountDeletionService accountDeletionService) : IRequestHandler<DeleteMyAccountCommand>
{
    public async ValueTask<Unit> Handle(DeleteMyAccountCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            throw new InvalidOperationException("Password is incorrect.");

        await accountDeletionService.DeleteAccountAsync(currentUser.UserId, deletionRequest: null, request.AccessToken, ct);
        return Unit.Value;
    }
}
