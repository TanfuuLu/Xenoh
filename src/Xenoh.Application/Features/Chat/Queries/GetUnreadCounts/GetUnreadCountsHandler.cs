using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Chat.Queries.GetUnreadCounts;

public sealed class GetUnreadCountsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetUnreadCountsQuery, IReadOnlyDictionary<Guid, int>>
{
    public async ValueTask<IReadOnlyDictionary<Guid, int>> Handle(
        GetUnreadCountsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var counts = await db.Messages
            .AsNoTracking()
            .Where(m =>
                (m.Relationship.ClientId == userId || m.Relationship.CoachId == userId) &&
                m.SenderId != userId &&
                !m.IsRead)
            .GroupBy(m => m.RelationshipId)
            .Select(g => new { RelationshipId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.RelationshipId, c => c.Count);
    }
}
