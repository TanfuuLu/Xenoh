using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Queries.GetPendingRequests;

public sealed class GetPendingRequestsHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetPendingRequestsQuery, List<CoachRelationshipResponse>>
{
    public async ValueTask<List<CoachRelationshipResponse>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        return await context.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.Coach)
            .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Pending)
            .Select(r => new CoachRelationshipResponse(
                r.Id,
                r.ClientId,
                r.Client.FirstName + " " + r.Client.LastName,
                r.CoachId,
                r.Coach.FirstName + " " + r.Coach.LastName,
                r.Status.ToString(),
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
