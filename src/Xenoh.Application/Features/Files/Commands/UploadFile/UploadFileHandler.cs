using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Files.Dtos;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService,
    IDocumentStorageService documentStorage
) : IRequestHandler<UploadFileCommand, StoredFileDto>
{
    public async ValueTask<StoredFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (request.Length <= 0)
            throw new InvalidOperationException("File is required.");

        var userId = currentUser.UserId;
        var tier = await subscriptionService.GetActiveTierAsync(userId, cancellationToken);

        var maxFileSize = SubscriptionLimits.MaxFileSizeBytes(tier);
        if (maxFileSize <= 0)
            throw new InvalidOperationException("File storage requires a Pro subscription.");

        if (request.Length > maxFileSize)
            throw new InvalidOperationException($"File must be {maxFileSize / (1024 * 1024)} MB or smaller.");

        var quota = SubscriptionLimits.MaxStorageBytes(tier);
        var usedBytes = await db.StoredFiles
            .Where(f => f.OwnerId == userId)
            .SumAsync(f => f.SizeBytes, cancellationToken);

        if (usedBytes + request.Length > quota)
            throw new InvalidOperationException(
                $"Storage quota exceeded. You have {FormatBytes(quota - usedBytes)} of {FormatBytes(quota)} remaining.");

        // SaveAsync also validates the document type by magic bytes.
        var storageKey = await documentStorage.SaveAsync(
            userId, request.FileName, request.ContentType, request.Content, cancellationToken);

        var file = new StoredFile
        {
            OwnerId = userId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.Length,
            StorageKey = storageKey
        };

        db.StoredFiles.Add(file);
        await db.SaveChangesAsync(cancellationToken);

        return new StoredFileDto(
            file.Id,
            file.FileName,
            file.ContentType,
            file.SizeBytes,
            file.CreatedAt,
            []);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (double)(1024L * 1024 * 1024):0.##} GB";
        return $"{bytes / (double)(1024 * 1024):0.##} MB";
    }
}
