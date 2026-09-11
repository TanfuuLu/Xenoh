using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.ListMyProgressCheckIns;

public sealed class ListMyProgressCheckInsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ListMyProgressCheckInsQuery, IReadOnlyList<ProgressCheckInDto>>
{
    public async ValueTask<IReadOnlyList<ProgressCheckInDto>> Handle(ListMyProgressCheckInsQuery request, CancellationToken ct) =>
        await db.ProgressCheckIns.AsNoTracking().Where(x => x.OwnerId == currentUser.UserId)
            .OrderByDescending(x => x.CheckInDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new ProgressCheckInDto(x.Id, x.CheckInDate, x.Title, x.Notes, x.BodyweightKg,
                x.IsMilestone, x.IsSharedWithCoach, x.CreatedAt,
                x.Photos.OrderBy(p => p.SortOrder).Select(p => new ProgressPhotoDto(p.Id, p.FileName, p.Angle, p.SortOrder)).ToList()))
            .ToListAsync(ct);
}
