using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressPhoto;

public sealed class DeleteProgressPhotoHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService storage) : IRequestHandler<DeleteProgressPhotoCommand>
{
    public async ValueTask<Unit> Handle(DeleteProgressPhotoCommand request, CancellationToken ct)
    {
        var checkIn = await db.ProgressCheckIns.Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == request.CheckInId && x.OwnerId == currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Check-in not found or access denied.");
        if (checkIn.Photos.Count == 1)
            throw new InvalidOperationException("A check-in must keep at least one photo. Delete the check-in instead.");

        var photo = checkIn.Photos.FirstOrDefault(x => x.Id == request.PhotoId)
            ?? throw new InvalidOperationException("Photo not found.");
        await storage.DeleteAsync(photo.StorageKey, ct);
        db.ProgressPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
