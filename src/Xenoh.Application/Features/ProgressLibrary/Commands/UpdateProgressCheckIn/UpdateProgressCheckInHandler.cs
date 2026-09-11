using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ProgressLibrary.Dtos;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Domain.Rules;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.UpdateProgressCheckIn;

public sealed class UpdateProgressCheckInHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptions) : IRequestHandler<UpdateProgressCheckInCommand, ProgressCheckInDto>
{
    public async ValueTask<ProgressCheckInDto> Handle(UpdateProgressCheckInCommand request, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;
        await ProgressLibraryAccess.EnsureCanCreateOrEditAsync(subscriptions, ownerId, ct);
        if (!ProgressLibraryRules.HasValidCheckInDate(request.CheckInDate))
            throw new InvalidOperationException("Check-in dates cannot be in the future.");
        if (request.BodyweightKg is <= 0 or > 500)
            throw new InvalidOperationException("Bodyweight must be between 0 and 500 kg.");
        if (request.Title?.Trim().Length > 120 || request.Notes?.Trim().Length > 2_000)
            throw new InvalidOperationException("The check-in title or note is too long.");

        var checkIn = await db.ProgressCheckIns.Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == request.CheckInId && x.OwnerId == ownerId, ct)
            ?? throw new InvalidOperationException("Check-in not found or access denied.");
        checkIn.CheckInDate = request.CheckInDate;
        checkIn.Title = Normalize(request.Title);
        checkIn.Notes = Normalize(request.Notes);
        checkIn.BodyweightKg = request.BodyweightKg;
        checkIn.IsMilestone = request.IsMilestone;
        checkIn.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ProgressCheckInDto(checkIn.Id, checkIn.CheckInDate, checkIn.Title, checkIn.Notes,
            checkIn.BodyweightKg, checkIn.IsMilestone, checkIn.IsSharedWithCoach, checkIn.CreatedAt,
            checkIn.Photos.OrderBy(x => x.SortOrder).Select(x => new ProgressPhotoDto(x.Id, x.FileName, x.Angle, x.SortOrder)).ToList());
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
