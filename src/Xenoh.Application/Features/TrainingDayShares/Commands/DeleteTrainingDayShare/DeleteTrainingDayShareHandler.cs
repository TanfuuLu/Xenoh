using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.DeleteTrainingDayShare;

public sealed class DeleteTrainingDayShareHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteTrainingDayShareCommand>
{
    public async ValueTask<Unit> Handle(DeleteTrainingDayShareCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var share = await db.TrainingDayShares
            .FirstOrDefaultAsync(s => s.Id == request.ShareId, cancellationToken)
            ?? throw new InvalidOperationException("Training day share not found.");

        if (share.UserId != userId)
            throw new UnauthorizedAccessException();

        db.TrainingDayShares.Remove(share);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
