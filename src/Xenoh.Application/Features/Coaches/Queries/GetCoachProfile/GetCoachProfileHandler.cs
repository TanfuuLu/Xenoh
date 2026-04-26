using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

public sealed class GetCoachProfileHandler(
    IApplicationDbContext context,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetCoachProfileQuery, CoachProfileResponse>
{
    public async ValueTask<CoachProfileResponse> Handle(GetCoachProfileQuery request, CancellationToken cancellationToken)
    {
        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");

        // Verify they are actually a coach
        var isCoach = await userManager.IsInRoleAsync(coach, UserRole.Coach);
        if (!isCoach)
            throw new InvalidOperationException("Coach not found.");

        var totalClients = await context.CoachClientRelationships
            .AsNoTracking()
            .CountAsync(r => r.CoachId == request.CoachId && r.Status == RelationshipStatus.Active, cancellationToken);

        return new CoachProfileResponse(
            coach.Id,
            $"{coach.FirstName} {coach.LastName}",
            coach.Email!,
            totalClients
        );
    }
}
