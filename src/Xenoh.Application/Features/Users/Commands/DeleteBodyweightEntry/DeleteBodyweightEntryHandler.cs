using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Users.Commands.DeleteBodyweightEntry;

public sealed class DeleteBodyweightEntryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteBodyweightEntryCommand>
{
    public async ValueTask<Unit> Handle(DeleteBodyweightEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var entry = await context.BodyweightLogs
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Bodyweight entry not found.");

        context.BodyweightLogs.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
