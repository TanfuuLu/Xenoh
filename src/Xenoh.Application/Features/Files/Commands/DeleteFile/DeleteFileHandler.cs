using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Files.Commands.DeleteFile;

public sealed class DeleteFileHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDocumentStorageService documentStorage
) : IRequestHandler<DeleteFileCommand>
{
    public async ValueTask<Unit> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.OwnerId == userId, cancellationToken)
            ?? throw new InvalidOperationException("File not found or access denied.");

        await documentStorage.DeleteAsync(file.StorageKey, cancellationToken);

        // Share rows cascade-delete via the FK relationship.
        db.StoredFiles.Remove(file);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
