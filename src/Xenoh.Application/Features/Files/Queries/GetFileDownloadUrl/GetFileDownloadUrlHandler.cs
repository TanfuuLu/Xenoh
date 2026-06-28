using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Queries.GetFileDownloadUrl;

public sealed class GetFileDownloadUrlHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService documentStorage
) : IRequestHandler<GetFileDownloadUrlQuery, FileDownloadUrlResponse>
{
    public async ValueTask<FileDownloadUrlResponse> Handle(
        GetFileDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // The caller may download if they own the file OR it was shared with them.
        var file = await db.StoredFiles
            .FirstOrDefaultAsync(
                f => f.Id == request.FileId
                     && (f.OwnerId == userId
                         || f.Shares.Any(s => s.SharedWithUserId == userId)),
                cancellationToken)
            ?? throw new InvalidOperationException("File not found or access denied.");

        var url = await documentStorage.GetPresignedDownloadUrlAsync(
            file.StorageKey, file.FileName, cancellationToken);

        return new FileDownloadUrlResponse(url);
    }
}
