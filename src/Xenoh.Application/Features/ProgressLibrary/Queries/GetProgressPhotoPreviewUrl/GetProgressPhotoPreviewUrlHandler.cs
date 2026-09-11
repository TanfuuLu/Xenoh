using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.GetProgressPhotoPreviewUrl;

public sealed class GetProgressPhotoPreviewUrlHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptions,
    IDocumentStorageService storage) : IRequestHandler<GetProgressPhotoPreviewUrlQuery, ProgressPhotoPreviewUrlDto>
{
    public async ValueTask<ProgressPhotoPreviewUrlDto> Handle(GetProgressPhotoPreviewUrlQuery request, CancellationToken ct)
    {
        var photo = await db.ProgressPhotos.AsNoTracking().Include(x => x.ProgressCheckIn)
            .FirstOrDefaultAsync(x => x.Id == request.PhotoId, ct)
            ?? throw new InvalidOperationException("Photo not found or access denied.");

        var viewerId = currentUser.UserId;
        if (photo.ProgressCheckIn.OwnerId != viewerId)
        {
            if (!photo.ProgressCheckIn.IsSharedWithCoach)
                throw new InvalidOperationException("Photo not found or access denied.");
            await ProgressLibraryAccess.EnsureCoachCanViewAsync(db, subscriptions, viewerId, photo.ProgressCheckIn.OwnerId, ct);
        }

        var url = await storage.GetPresignedDownloadUrlAsync(photo.StorageKey, photo.FileName, ct, inline: true);
        return new ProgressPhotoPreviewUrlDto(url);
    }
}
