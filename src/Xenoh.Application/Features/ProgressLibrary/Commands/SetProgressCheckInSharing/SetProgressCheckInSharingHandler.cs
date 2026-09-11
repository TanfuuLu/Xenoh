using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.ProgressLibrary.Dtos;
using Xenoh.Application.Features.Subscriptions;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.SetProgressCheckInSharing;

public sealed class SetProgressCheckInSharingHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptions) : IRequestHandler<SetProgressCheckInSharingCommand, ProgressCheckInDto>
{
    public async ValueTask<ProgressCheckInDto> Handle(SetProgressCheckInSharingCommand request, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;
        await ProgressLibraryAccess.EnsureCanCreateOrEditAsync(subscriptions, ownerId, ct);
        var checkIn = await db.ProgressCheckIns.Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == request.CheckInId && x.OwnerId == ownerId, ct)
            ?? throw new InvalidOperationException("Check-in not found or access denied.");

        if (request.IsSharedWithCoach)
        {
            var hasActiveCoach = await db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow)
                .AnyAsync(x => x.ClientId == ownerId, ct);
            if (!hasActiveCoach)
                throw new InvalidOperationException("Connect with an active coach before sharing a check-in.");
        }

        checkIn.IsSharedWithCoach = request.IsSharedWithCoach;
        checkIn.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ProgressCheckInDto(checkIn.Id, checkIn.CheckInDate, checkIn.Title, checkIn.Notes,
            checkIn.BodyweightKg, checkIn.IsMilestone, checkIn.IsSharedWithCoach, checkIn.CreatedAt,
            checkIn.Photos.OrderBy(x => x.SortOrder).Select(x => new ProgressPhotoDto(x.Id, x.FileName, x.Angle, x.SortOrder)).ToList());
    }
}
