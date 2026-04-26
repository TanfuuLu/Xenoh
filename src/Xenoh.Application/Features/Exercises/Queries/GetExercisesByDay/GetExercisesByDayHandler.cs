using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

public sealed class GetExercisesByDayHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisesByDayQuery, List<ExerciseResponse>>
{
    public async ValueTask<List<ExerciseResponse>> Handle(
        GetExercisesByDayQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await exerciseRepo.GetByDayWithPrsAsync(request.DailyWorkoutId, userId, cancellationToken);
    }
}
