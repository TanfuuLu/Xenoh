using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Users.Commands.DeleteBodyweightEntry;

public sealed class DeleteBodyweightEntryHandler(
    IBodyweightRepository bodyweightRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteBodyweightEntryCommand>
{
    public async ValueTask<Unit> Handle(DeleteBodyweightEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var entry = await bodyweightRepo.FindByIdAndOwnerAsync(request.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Bodyweight entry not found.");

        bodyweightRepo.Remove(entry);
        await bodyweightRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
