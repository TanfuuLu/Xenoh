using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

public sealed class GetWeeksByPlanHandler(
    IWeeklyWorkoutRepository weeklyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetWeeksByPlanQuery, List<WeeklyWorkoutResponse>>
{
    public async ValueTask<List<WeeklyWorkoutResponse>> Handle(
        GetWeeksByPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var accessible = await weeklyWorkoutRepo.PlanAccessibleByUserAsync(
            request.PlanId, userId, cancellationToken);

        if (!accessible)
            throw new InvalidOperationException("Plan not found.");

        return await weeklyWorkoutRepo.GetByPlanAsync(request.PlanId, cancellationToken);
    }
}
