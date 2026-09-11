using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ProgressLibrary.Dtos;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.CreateProgressCheckIn;

public sealed class CreateProgressCheckInHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptions,
    IDocumentStorageService storage) : IRequestHandler<CreateProgressCheckInCommand, ProgressCheckInDto>
{
    public async ValueTask<ProgressCheckInDto> Handle(CreateProgressCheckInCommand request, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;
        await ProgressLibraryAccess.EnsureCanCreateOrEditAsync(subscriptions, ownerId, ct);
        Validate(request);

        var tier = await subscriptions.GetActiveTierAsync(ownerId, ct);
        var usedBytes = await db.StoredFiles.Where(x => x.OwnerId == ownerId).SumAsync(x => x.SizeBytes, ct)
            + await db.ProgressPhotos.Where(x => x.ProgressCheckIn.OwnerId == ownerId).SumAsync(x => x.SizeBytes, ct);
        var incomingBytes = request.Photos.Sum(x => x.Length);
        var quota = SubscriptionLimits.MaxStorageBytes(tier);
        if (usedBytes + incomingBytes > quota)
            throw new InvalidOperationException("Storage quota exceeded. Delete files or progress photos before uploading more.");

        var checkIn = new ProgressCheckIn
        {
            OwnerId = ownerId,
            CheckInDate = request.CheckInDate,
            Title = Normalize(request.Title),
            Notes = Normalize(request.Notes),
            BodyweightKg = request.BodyweightKg,
            IsMilestone = request.IsMilestone
        };
        var savedKeys = new List<string>();

        try
        {
            for (var index = 0; index < request.Photos.Count; index++)
            {
                var upload = request.Photos[index];
                var key = await storage.SaveProgressPhotoAsync(ownerId, upload.FileName, upload.ContentType, upload.Content, ct);
                savedKeys.Add(key);
                checkIn.Photos.Add(new ProgressPhoto
                {
                    FileName = upload.FileName,
                    ContentType = upload.ContentType,
                    SizeBytes = upload.Length,
                    StorageKey = key,
                    Angle = upload.Angle,
                    SortOrder = index
                });
            }

            db.ProgressCheckIns.Add(checkIn);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            foreach (var key in savedKeys)
                await storage.DeleteAsync(key, ct);
            throw;
        }

        return new ProgressCheckInDto(checkIn.Id, checkIn.CheckInDate, checkIn.Title, checkIn.Notes,
            checkIn.BodyweightKg, checkIn.IsMilestone, checkIn.IsSharedWithCoach, checkIn.CreatedAt,
            checkIn.Photos.OrderBy(x => x.SortOrder).Select(x => new ProgressPhotoDto(x.Id, x.FileName, x.Angle, x.SortOrder)).ToList());
    }

    private static void Validate(CreateProgressCheckInCommand request)
    {
        if (!ProgressLibraryRules.HasValidPhotoCount(request.Photos.Count))
            throw new InvalidOperationException("Add between 1 and 3 photos to each check-in.");
        if (!ProgressLibraryRules.HasValidCheckInDate(request.CheckInDate))
            throw new InvalidOperationException("Check-in dates cannot be in the future.");
        if (request.Photos.Any(x => x.Length <= 0 || x.Length > ProgressLibraryRules.MaximumPhotoSizeBytes))
            throw new InvalidOperationException("Each photo must be 5 MB or smaller.");
        if (request.BodyweightKg is <= 0 or > 500)
            throw new InvalidOperationException("Bodyweight must be between 0 and 500 kg.");
        if (request.Title?.Trim().Length > 120 || request.Notes?.Trim().Length > 2_000)
            throw new InvalidOperationException("The check-in title or note is too long.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
