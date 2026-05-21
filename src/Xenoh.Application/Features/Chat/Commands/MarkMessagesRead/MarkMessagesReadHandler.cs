using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Chat.Commands.MarkMessagesRead;

public sealed class MarkMessagesReadHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<MarkMessagesReadCommand>
{
    public async ValueTask<Unit> Handle(
        MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        await db.Messages
            .Where(m => m.RelationshipId == request.RelationshipId
                        && m.SenderId != userId
                        && !m.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsRead, true),
                cancellationToken);

        return Unit.Value;
    }
}
