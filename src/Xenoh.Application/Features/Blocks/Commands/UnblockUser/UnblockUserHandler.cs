using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Blocks.Commands.UnblockUser;

public sealed class UnblockUserHandler(
    IUserBlockRepository blockRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UnblockUserCommand>
{
    public async ValueTask<Unit> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var blockerId = currentUser.UserId;

        var block = await blockRepo.FindAsync(blockerId, request.TargetUserId, cancellationToken);
        if (block is null)
            return Unit.Value;

        blockRepo.Remove(block);
        await blockRepo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
