using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByWeek;

public sealed class GetExercisesByWeekHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisesByWeekQuery, List<ExerciseResponse>>
{
    public async ValueTask<List<ExerciseResponse>> Handle(
        GetExercisesByWeekQuery request, CancellationToken cancellationToken)
    {
        return await exerciseRepo.GetByWeekWithPrsAsync(
            request.WeeklyWorkoutId,
            currentUser.UserId,
            cancellationToken);
    }
}
