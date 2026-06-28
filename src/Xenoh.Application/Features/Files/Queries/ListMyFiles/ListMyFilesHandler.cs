using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Files.Dtos;
using Xenoh.Application.Features.Subscriptions;

namespace Xenoh.Application.Features.Files.Queries.ListMyFiles;

public sealed class ListMyFilesHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<ListMyFilesQuery, MyFilesResponse>
{
    public async ValueTask<MyFilesResponse> Handle(ListMyFilesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var tier = await subscriptionService.GetActiveTierAsync(userId, cancellationToken);

        var files = await db.StoredFiles
            .Where(f => f.OwnerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new StoredFileDto(
                f.Id,
                f.FileName,
                f.ContentType,
                f.SizeBytes,
                f.CreatedAt,
                f.Shares
                    .Select(s => new FileShareTargetDto(
                        s.Id,
                        s.SharedWithUserId,
                        (s.SharedWithUser.FirstName + " " + s.SharedWithUser.LastName).Trim()))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var usedBytes = files.Sum(f => f.SizeBytes);

        return new MyFilesResponse(
            usedBytes,
            SubscriptionLimits.MaxStorageBytes(tier),
            SubscriptionLimits.MaxFileSizeBytes(tier),
            files);
    }
}
