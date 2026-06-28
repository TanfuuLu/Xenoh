using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Files.Commands.UnshareFile;

public sealed class UnshareFileHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UnshareFileCommand>
{
    public async ValueTask<Unit> Handle(UnshareFileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var share = await db.StoredFileShares.FirstOrDefaultAsync(
            s => s.Id == request.ShareId
                 && s.FileId == request.FileId
                 && s.SharedByUserId == userId,
            cancellationToken)
            ?? throw new InvalidOperationException("Share not found or access denied.");

        db.StoredFileShares.Remove(share);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
