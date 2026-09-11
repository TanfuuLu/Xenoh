using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressCheckIn;

public sealed class DeleteProgressCheckInHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService storage) : IRequestHandler<DeleteProgressCheckInCommand>
{
    public async ValueTask<Unit> Handle(DeleteProgressCheckInCommand request, CancellationToken ct)
    {
        var checkIn = await db.ProgressCheckIns.Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == request.CheckInId && x.OwnerId == currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Check-in not found or access denied.");

        foreach (var photo in checkIn.Photos)
            await storage.DeleteAsync(photo.StorageKey, ct);

        db.ProgressCheckIns.Remove(checkIn);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
