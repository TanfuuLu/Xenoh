using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;

public sealed class GetMyCoachHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyCoachQuery, CoachRelationshipResponse?>
{
    public async ValueTask<CoachRelationshipResponse?> Handle(GetMyCoachQuery request, CancellationToken cancellationToken)
    {
        var clientId = currentUser.UserId;

        var relationship = await context.CoachClientRelationships
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.Coach)
            .FirstOrDefaultAsync(r => r.ClientId == clientId, cancellationToken);

        if (relationship is null) return null;

        return new CoachRelationshipResponse(
            relationship.Id,
            relationship.ClientId,
            $"{relationship.Client.FirstName} {relationship.Client.LastName}",
            relationship.CoachId,
            $"{relationship.Coach.FirstName} {relationship.Coach.LastName}",
            relationship.Status.ToString(),
            relationship.CreatedAt
        );
    }
}
