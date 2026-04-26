using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

public sealed class GetCoachProfileHandler(
    ICoachClientRepository coachClientRepo,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetCoachProfileQuery, CoachProfileResponse>
{
    public async ValueTask<CoachProfileResponse> Handle(GetCoachProfileQuery request, CancellationToken cancellationToken)
    {
        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");

        var isCoach = await userManager.IsInRoleAsync(coach, UserRole.Coach);
        if (!isCoach)
            throw new InvalidOperationException("Coach not found.");

        var totalClients = await coachClientRepo.CountActiveByCoachAsync(request.CoachId, cancellationToken);

        return new CoachProfileResponse(
            coach.Id,
            $"{coach.FirstName} {coach.LastName}",
            coach.Email!,
            totalClients
        );
    }
}
