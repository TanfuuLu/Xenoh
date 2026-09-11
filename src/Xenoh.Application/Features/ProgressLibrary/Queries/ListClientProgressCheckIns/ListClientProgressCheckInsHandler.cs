using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.ListClientProgressCheckIns;

public sealed class ListClientProgressCheckInsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptions) : IRequestHandler<ListClientProgressCheckInsQuery, IReadOnlyList<ProgressCheckInDto>>
{
    public async ValueTask<IReadOnlyList<ProgressCheckInDto>> Handle(ListClientProgressCheckInsQuery request, CancellationToken ct)
    {
        await ProgressLibraryAccess.EnsureCoachCanViewAsync(db, subscriptions, currentUser.UserId, request.ClientId, ct);
        return await db.ProgressCheckIns.AsNoTracking()
            .Where(x => x.OwnerId == request.ClientId && x.IsSharedWithCoach)
            .OrderByDescending(x => x.CheckInDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new ProgressCheckInDto(x.Id, x.CheckInDate, x.Title, x.Notes, x.BodyweightKg,
                x.IsMilestone, x.IsSharedWithCoach, x.CreatedAt,
                x.Photos.OrderBy(p => p.SortOrder).Select(p => new ProgressPhotoDto(p.Id, p.FileName, p.Angle, p.SortOrder)).ToList()))
            .ToListAsync(ct);
    }
}
