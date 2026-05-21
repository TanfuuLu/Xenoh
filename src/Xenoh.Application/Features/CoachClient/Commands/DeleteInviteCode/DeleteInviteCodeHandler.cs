using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.CoachClient.Commands.DeleteInviteCode;

public sealed class DeleteInviteCodeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteInviteCodeCommand>
{
    public async ValueTask<Unit> Handle(DeleteInviteCodeCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var code = await db.CoachInviteCodes
            .FirstOrDefaultAsync(c => c.Id == request.InviteCodeId && c.CoachId == coachId, cancellationToken)
            ?? throw new InvalidOperationException("Invite code not found.");

        if (code.IsUsed)
            throw new InvalidOperationException("Cannot delete a code that has already been used.");

        db.CoachInviteCodes.Remove(code);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
