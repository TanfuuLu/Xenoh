using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.MarkDayStatus;

public sealed class MarkDayStatusHandler(
    IDailyWorkoutRepository dailyWorkoutRepo,
    ICurrentUserService currentUser
) : IRequestHandler<MarkDayStatusCommand>
{
    public async ValueTask<Unit> Handle(MarkDayStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var day = await dailyWorkoutRepo.FindWithPlanAsync(request.DailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Daily workout not found.");

        var plan = day.WeeklyWorkout.Plan;

        if (plan.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the plan owner can mark day status.");

        day.Status = request.Status;
        day.UpdatedAt = DateTime.UtcNow;

        await dailyWorkoutRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
