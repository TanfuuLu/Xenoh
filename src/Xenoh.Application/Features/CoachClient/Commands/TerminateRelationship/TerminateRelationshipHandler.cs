using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;

public sealed class TerminateRelationshipHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<TerminateRelationshipCommand>
{
    public async ValueTask<Unit> Handle(TerminateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await context.CoachClientRelationships
            .FirstOrDefaultAsync(r =>
                r.Id == request.RelationshipId &&
                (r.ClientId == userId || r.CoachId == userId), cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        // Delete all plans created by this coach for the client
        if (relationship.Status == RelationshipStatus.Accepted)
        {
            var coachPlans = await context.Plans
                .Where(p => p.OwnerId == relationship.ClientId
                            && p.CreatedByCoachId == relationship.CoachId
                            && p.PlanType == PlanType.Coach)
                .ToListAsync(cancellationToken);

            context.Plans.RemoveRange(coachPlans);
        }

        context.CoachClientRelationships.Remove(relationship);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
