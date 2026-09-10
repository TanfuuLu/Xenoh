using Mediator;
using Xenoh.Application.Features.CoachClient;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Queries.ListSharedWithMe;

public sealed class ListSharedWithMeHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<ListSharedWithMeQuery, List<SharedFileDto>>
{
    public async ValueTask<List<SharedFileDto>> Handle(
        ListSharedWithMeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        return await db.StoredFileShares
            .AsNoTracking()
            .Where(s => s.SharedWithUserId == userId && db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow).Any(r =>
                (r.CoachId == s.SharedByUserId && r.ClientId == userId) || (r.ClientId == s.SharedByUserId && r.CoachId == userId)))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SharedFileDto(
                s.File.Id,
                s.File.FileName,
                s.File.ContentType,
                s.File.SizeBytes,
                s.File.CreatedAt,
                (s.File.Owner.FirstName + " " + s.File.Owner.LastName).Trim()))
            .ToListAsync(cancellationToken);
    }
}
